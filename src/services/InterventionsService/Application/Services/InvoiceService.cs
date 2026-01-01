using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Savora.InterventionsService.Domain.Entities;
using Savora.InterventionsService.Infrastructure.Data;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Interventions;
using Savora.Shared.Enums;

namespace Savora.InterventionsService.Application.Services;

public class InvoiceServiceImpl : IInvoiceService
{
    private readonly InterventionsDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<InvoiceServiceImpl> _logger;
    private readonly IConfiguration _configuration;
    private readonly IReclamationApiClient _reclamationApiClient;
    private readonly IClientApiClient _clientApiClient;

    public InvoiceServiceImpl(
        InterventionsDbContext context,
        IEmailService emailService,
        ILogger<InvoiceServiceImpl> logger,
        IConfiguration configuration,
        IReclamationApiClient reclamationApiClient,
        IClientApiClient clientApiClient)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
        _reclamationApiClient = reclamationApiClient;
        _clientApiClient = clientApiClient;

        // QuestPDF License
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<ApiResponse<InvoiceDetailDto>> GetByIdAsync(Guid id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Intervention)
                .ThenInclude(i => i.PartsUsed)
            .Include(i => i.Intervention)
                .ThenInclude(i => i.Labor)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return ApiResponse<InvoiceDetailDto>.FailureResponse("Invoice not found");
        }

        var dto = await MapToDetailDtoAsync(invoice);
        return ApiResponse<InvoiceDetailDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<InvoiceDto>> GetByInterventionIdAsync(Guid interventionId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Intervention)
            .FirstOrDefaultAsync(i => i.InterventionId == interventionId);

        if (invoice == null)
        {
            return ApiResponse<InvoiceDto>.FailureResponse("Invoice not found for this intervention");
        }

        return ApiResponse<InvoiceDto>.SuccessResponse(MapToDto(invoice));
    }

    public async Task<ApiResponse<PaginatedResult<InvoiceListDto>>> GetAllAsync(PaginationParams pagination)
    {
        var query = _context.Invoices
            .Include(i => i.Intervention)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        query = query.OrderByDescending(i => i.CreatedAt);

        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var dtos = new List<InvoiceListDto>();
        foreach (var invoice in items)
        {
            dtos.Add(await MapToListDtoAsync(invoice));
        }

        var result = new PaginatedResult<InvoiceListDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
        return ApiResponse<PaginatedResult<InvoiceListDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<PaginatedResult<InvoiceListDto>>> GetByClientIdAsync(Guid clientId, PaginationParams pagination)
    {
        // Get all invoices
        var allInvoices = await _context.Invoices
            .Include(i => i.Intervention)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        // Filter invoices by checking if the reclamation belongs to the client
        var clientInvoices = new List<Invoice>();
        foreach (var invoice in allInvoices)
        {
            if (invoice.Intervention == null) continue;
            
            try
            {
                var reclamation = await _reclamationApiClient.GetReclamationAsync(invoice.Intervention.ReclamationId);
                if (reclamation != null && reclamation.ClientId == clientId)
                {
                    clientInvoices.Add(invoice);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch reclamation {ReclamationId} for invoice filtering", invoice.Intervention.ReclamationId);
            }
        }

        var totalCount = clientInvoices.Count;

        // Apply pagination
        var items = clientInvoices
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToList();

        var dtos = new List<InvoiceListDto>();
        foreach (var invoice in items)
        {
            dtos.Add(await MapToListDtoAsync(invoice));
        }

        var result = new PaginatedResult<InvoiceListDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
        return ApiResponse<PaginatedResult<InvoiceListDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<InvoiceDto>> GenerateInvoiceAsync(Guid interventionId, string? clientEmail = null, string? clientName = null)
    {
        var intervention = await _context.Interventions
            .Include(i => i.PartsUsed)
            .Include(i => i.Labor)
            .Include(i => i.Invoice)
            .Include(i => i.Technician)
            .FirstOrDefaultAsync(i => i.Id == interventionId);

        if (intervention == null)
        {
            return ApiResponse<InvoiceDto>.FailureResponse("Intervention not found");
        }

        if (intervention.Status != InterventionStatus.Completed)
        {
            return ApiResponse<InvoiceDto>.FailureResponse("Cannot generate invoice for incomplete intervention");
        }

        if (intervention.Invoice != null)
        {
            return ApiResponse<InvoiceDto>.FailureResponse("Invoice already exists for this intervention");
        }

        var invoiceNumber = await GenerateInvoiceNumberAsync();
        var partsTotal = intervention.PartsUsed?.Sum(p => p.TotalPrice) ?? 0;
        var laborTotal = intervention.Labor?.TotalAmount ?? 0;
        var totalAmount = intervention.IsFree ? 0 : partsTotal + laborTotal;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InterventionId = interventionId,
            InvoiceNumber = invoiceNumber,
            IsFree = intervention.IsFree,
            PartsTotal = partsTotal,
            LaborTotal = laborTotal,
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Generate PDF
        try
        {
            var pdfPath = await GenerateInvoicePdfAsync(invoice, intervention);
            invoice.PdfPath = pdfPath;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF for invoice {InvoiceId}", invoice.Id);
        }

        // Send invoice email to client
        if (!string.IsNullOrEmpty(clientEmail))
        {
            try
            {
                await _emailService.SendInvoiceReadyEmailAsync(
                    clientEmail,
                    clientName ?? "Client",
                    invoiceNumber,
                    totalAmount
                );
                _logger.LogInformation("Invoice email sent to {Email}", clientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send invoice email");
            }
        }

        _logger.LogInformation("Invoice {InvoiceNumber} generated for intervention {InterventionId}", invoiceNumber, interventionId);
        return ApiResponse<InvoiceDto>.SuccessResponse(MapToDto(invoice), "Invoice generated successfully");
    }

    public async Task<ApiResponse<InvoiceDto>> GenerateInvoiceFromOrderAsync(Guid orderId, decimal totalAmount, string orderNumber, string? clientEmail = null, string? clientName = null)
    {
        // Check if invoice already exists for this order
        var existingInvoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.OrderId == orderId);
        
        if (existingInvoice != null)
        {
            return ApiResponse<InvoiceDto>.FailureResponse("Invoice already exists for this order");
        }

        var invoiceNumber = await GenerateInvoiceNumberAsync();

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            InvoiceNumber = invoiceNumber,
            IsFree = false,
            PartsTotal = 0,
            LaborTotal = 0,
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Generate PDF for order invoice
        try
        {
            var pdfPath = await GenerateOrderInvoicePdfAsync(invoice, orderNumber, totalAmount, clientName);
            invoice.PdfPath = pdfPath;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF for order invoice {InvoiceId}", invoice.Id);
        }

        // Send invoice email to client
        if (!string.IsNullOrEmpty(clientEmail))
        {
            try
            {
                await _emailService.SendInvoiceReadyEmailAsync(
                    clientEmail,
                    clientName ?? "Client",
                    invoiceNumber,
                    totalAmount
                );
                _logger.LogInformation("Order invoice email sent to {Email}", clientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send order invoice email");
            }
        }

        _logger.LogInformation("Invoice {InvoiceNumber} generated for order {OrderId}", invoiceNumber, orderId);
        return ApiResponse<InvoiceDto>.SuccessResponse(MapToDto(invoice), "Invoice generated successfully");
    }

    public async Task<ApiResponse<byte[]>> GetInvoicePdfAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Intervention)
                .ThenInclude(i => i.PartsUsed)
            .Include(i => i.Intervention)
                .ThenInclude(i => i.Labor)
            .Include(i => i.Intervention)
                .ThenInclude(i => i.Technician)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
        {
            return ApiResponse<byte[]>.FailureResponse("Invoice not found");
        }

        if (invoice.Intervention != null)
        {
            var pdfBytes = GenerateInvoicePdfBytes(invoice, invoice.Intervention);
            return ApiResponse<byte[]>.SuccessResponse(pdfBytes);
        }
        else if (invoice.OrderId != null)
        {
            // For order invoices, generate a simple PDF
            // We'll need order details from ArticlesService, but for now use basic info
            var pdfBytes = GenerateOrderInvoicePdfBytes(invoice, "ORDER", invoice.TotalAmount, "Client");
            return ApiResponse<byte[]>.SuccessResponse(pdfBytes);
        }
        else
        {
            return ApiResponse<byte[]>.FailureResponse("Invoice has no associated intervention or order");
        }
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;
        var count = await _context.Invoices
            .CountAsync(i => i.CreatedAt.Year == year && i.CreatedAt.Month == month);

        return $"INV-{year}{month:D2}-{(count + 1):D4}";
    }

    private async Task<string> GenerateInvoicePdfAsync(Invoice invoice, Intervention intervention)
    {
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "invoices");
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{invoice.InvoiceNumber}.pdf";
        var filePath = Path.Combine(uploadsPath, fileName);

        var pdfBytes = GenerateInvoicePdfBytes(invoice, intervention);
        await File.WriteAllBytesAsync(filePath, pdfBytes);

        return $"/invoices/{fileName}";
    }

    private byte[] GenerateInvoicePdfBytes(Invoice invoice, Intervention intervention)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(ComposeHeader);

                page.Content().Element(c => ComposeContent(c, invoice, intervention));

                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private async Task<string> GenerateOrderInvoicePdfAsync(Invoice invoice, string orderNumber, decimal totalAmount, string? clientName)
    {
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "invoices");
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{invoice.InvoiceNumber}.pdf";
        var filePath = Path.Combine(uploadsPath, fileName);

        var pdfBytes = GenerateOrderInvoicePdfBytes(invoice, orderNumber, totalAmount, clientName);
        await File.WriteAllBytesAsync(filePath, pdfBytes);

        return $"/invoices/{fileName}";
    }

    private byte[] GenerateOrderInvoicePdfBytes(Invoice invoice, string orderNumber, decimal totalAmount, string? clientName)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(ComposeHeader);

                page.Content().Element(c => ComposeOrderContent(c, invoice, orderNumber, totalAmount, clientName));

                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeOrderContent(IContainer container, Invoice invoice, string orderNumber, decimal totalAmount, string? clientName)
    {
        container.PaddingVertical(20).Column(column =>
        {
            // Invoice info
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Facture pour commande:").FontSize(10).FontColor(Colors.Grey.Darken1);
                    c.Item().Text(orderNumber).FontSize(14).Bold();
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().AlignRight().Text($"N° Facture: {invoice.InvoiceNumber}").FontSize(10).Bold();
                    c.Item().AlignRight().Text($"Date: {invoice.CreatedAt:dd/MM/yyyy}").FontSize(10);
                });
            });

            column.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

            // Client info
            column.Item().PaddingTop(20).Text("Client:").FontSize(12).Bold();
            column.Item().PaddingTop(5).Text(clientName ?? "Client").FontSize(11);

            column.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

            // Order details
            column.Item().PaddingTop(20).Text("Détails de la commande:").FontSize(12).Bold();
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Description").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Montant").Bold();
                });

                table.Cell().Element(CellStyle).Text("Commande " + orderNumber);
                table.Cell().Element(CellStyle).AlignRight().Text($"{totalAmount:N2} TND");
            });

            column.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

            // Total
            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(150).Column(c =>
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Total TTC:").FontSize(12).Bold();
                        r.ConstantItem(100).AlignRight().Text($"{totalAmount:N2} TND").FontSize(12).Bold();
                    });
                });
            });
        });
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8)
            .Background(Colors.White);
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("SAVORA")
                    .FontSize(28)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().Text("Smart After-Sales Service, Simplified.")
                    .FontSize(10)
                    .Italic()
                    .FontColor(Colors.Grey.Darken1);
            });

            row.RelativeItem().Column(column =>
            {
                column.Item().AlignRight().Text("FACTURE")
                    .FontSize(24)
                    .Bold();
            });
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice, Intervention intervention)
    {
        container.PaddingVertical(20).Column(column =>
        {
            // Invoice info
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Facture N°: {invoice.InvoiceNumber}").Bold();
                    c.Item().Text($"Date: {invoice.CreatedAt:dd/MM/yyyy}");
                    c.Item().Text($"Intervention: {intervention.Id.ToString()[..8].ToUpper()}");
                });

                if (invoice.IsFree)
                {
                    row.RelativeItem().AlignRight().Text("SOUS GARANTIE")
                        .FontSize(14)
                        .Bold()
                        .FontColor(Colors.Green.Darken2);
                }
            });

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Technician info
            if (intervention.Technician != null)
            {
                column.Item().Text($"Technicien: {intervention.Technician.FullName}");
                column.Item().Text($"Date d'intervention: {intervention.PlannedDate:dd/MM/yyyy}");
            }

            column.Item().PaddingVertical(10);

            // Parts table
            if (intervention.PartsUsed != null && intervention.PartsUsed.Any())
            {
                column.Item().Text("Pièces Utilisées").FontSize(14).Bold();
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Article").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Qté").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("P.U.").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Total").Bold();
                    });

                    foreach (var part in intervention.PartsUsed)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(part.PartName);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(part.Quantity.ToString());
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{part.UnitPriceSnapshot:N2} TND");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{part.TotalPrice:N2} TND");
                    }
                });

                column.Item().PaddingVertical(10);
            }

            // Labor
            if (intervention.Labor != null)
            {
                column.Item().Text("Main d'œuvre").FontSize(14).Bold();
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text($"Heures: {intervention.Labor.Hours:N2} h × {intervention.Labor.HourlyRate:N2} TND/h");
                    row.RelativeItem().AlignRight().Text($"{intervention.Labor.TotalAmount:N2} TND");
                });

                column.Item().PaddingVertical(10);
            }

            column.Item().LineHorizontal(2).LineColor(Colors.Blue.Darken2);

            // Totals
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(200).Column(c =>
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Sous-total Pièces:");
                        r.RelativeItem().AlignRight().Text($"{invoice.PartsTotal:N2} TND");
                    });
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Sous-total Main d'œuvre:");
                        r.RelativeItem().AlignRight().Text($"{invoice.LaborTotal:N2} TND");
                    });
                    c.Item().PaddingTop(5).Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL:").FontSize(14).Bold();
                        r.RelativeItem().AlignRight().Text($"{invoice.TotalAmount:N2} TND").FontSize(14).Bold();
                    });
                });
            });

            if (invoice.IsFree)
            {
                column.Item().PaddingTop(20).Background(Colors.Green.Lighten4).Padding(10).Text(
                    "Cette intervention est couverte par la garantie. Aucun montant n'est dû.")
                    .Italic()
                    .FontColor(Colors.Green.Darken2);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("SAVORA - Service Après-Vente").FontSize(9).FontColor(Colors.Grey.Medium);
                row.RelativeItem().AlignCenter().Text("www.savora.tn").FontSize(9).FontColor(Colors.Grey.Medium);
                row.RelativeItem().AlignRight().Text("contact@savora.tn").FontSize(9).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private async Task<InvoiceListDto> MapToListDtoAsync(Invoice invoice)
    {
        if (invoice.Intervention == null)
        {
            return new InvoiceListDto
            {
                Id = invoice.Id,
                InterventionId = invoice.InterventionId ?? Guid.Empty,
                InvoiceNumber = invoice.InvoiceNumber,
                ClientName = "Client",
                ReclamationTitle = "Réclamation",
                IsFree = invoice.IsFree,
                PartsTotal = invoice.PartsTotal,
                LaborTotal = invoice.LaborTotal,
                TotalAmount = invoice.TotalAmount,
                CreatedAt = invoice.CreatedAt
            };
        }

        var reclamation = await _reclamationApiClient.GetReclamationAsync(invoice.Intervention.ReclamationId);
        
        return new InvoiceListDto
        {
            Id = invoice.Id,
            InterventionId = invoice.InterventionId ?? Guid.Empty,
            InvoiceNumber = invoice.InvoiceNumber,
            ClientName = reclamation?.ClientName ?? "Client",
            ReclamationTitle = reclamation?.Title ?? "Réclamation",
            IsFree = invoice.IsFree,
            PartsTotal = invoice.PartsTotal,
            LaborTotal = invoice.LaborTotal,
            TotalAmount = invoice.TotalAmount,
            CreatedAt = invoice.CreatedAt
        };
    }

    private async Task<InvoiceDetailDto> MapToDetailDtoAsync(Invoice invoice)
    {
        if (invoice.Intervention == null)
        {
            return new InvoiceDetailDto
            {
                Id = invoice.Id,
                InterventionId = invoice.InterventionId ?? Guid.Empty,
                InvoiceNumber = invoice.InvoiceNumber,
                ClientName = "Client",
                ClientEmail = "",
                ClientPhone = "",
                ClientAddress = null,
                ReclamationTitle = "Réclamation",
                IsFree = invoice.IsFree,
                Parts = new List<PartUsedDto>(),
                Labor = new List<LaborDto>(),
                PartsTotal = invoice.PartsTotal,
                LaborTotal = invoice.LaborTotal,
                TotalAmount = invoice.TotalAmount,
                PdfPath = invoice.PdfPath,
                CreatedAt = invoice.CreatedAt
            };
        }

        var reclamation = await _reclamationApiClient.GetReclamationAsync(invoice.Intervention.ReclamationId);
        
        return new InvoiceDetailDto
        {
            Id = invoice.Id,
            InterventionId = invoice.InterventionId ?? Guid.Empty,
            InvoiceNumber = invoice.InvoiceNumber,
            ClientName = reclamation?.ClientName ?? "Client",
            ClientEmail = reclamation?.ClientEmail ?? "",
            ClientPhone = reclamation?.ClientPhone ?? "",
            ClientAddress = null, // ReclamationDto doesn't have Address field
            ReclamationTitle = reclamation?.Title ?? "Réclamation",
            IsFree = invoice.IsFree,
            Parts = invoice.Intervention.PartsUsed?.Select(p => new PartUsedDto
            {
                Id = p.Id,
                PartId = p.PartId,
                PartName = p.PartName,
                PartReference = p.PartReference,
                Quantity = p.Quantity,
                UnitPrice = p.UnitPriceSnapshot,
                TotalPrice = p.TotalPrice
            }).ToList() ?? new List<PartUsedDto>(),
            Labor = invoice.Intervention.Labor != null ? new List<LaborDto>
            {
                new LaborDto
                {
                    Id = invoice.Intervention.Labor.Id,
                    Hours = invoice.Intervention.Labor.Hours,
                    HourlyRate = invoice.Intervention.Labor.HourlyRate,
                    TotalAmount = invoice.Intervention.Labor.TotalAmount,
                    Description = invoice.Intervention.Labor.Description
                }
            } : new List<LaborDto>(),
            PartsTotal = invoice.PartsTotal,
            LaborTotal = invoice.LaborTotal,
            TotalAmount = invoice.TotalAmount,
            PdfPath = invoice.PdfPath,
            CreatedAt = invoice.CreatedAt
        };
    }

    private static InvoiceDto MapToDto(Invoice invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            InterventionId = invoice.InterventionId ?? Guid.Empty,
            InvoiceNumber = invoice.InvoiceNumber,
            IsFree = invoice.IsFree,
            PartsTotal = invoice.PartsTotal,
            LaborTotal = invoice.LaborTotal,
            TotalAmount = invoice.TotalAmount,
            PdfPath = invoice.PdfPath,
            CreatedAt = invoice.CreatedAt
        };
    }
}

