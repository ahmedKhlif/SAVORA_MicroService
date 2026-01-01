// Dashboard Export Functions
window.dashboardExport = {
    // Export dashboard to PDF using html2pdf library
    exportToPdf: function(elementId, filename) {
        const element = document.getElementById(elementId);
        if (!element) {
            console.error('Element not found for PDF export');
            return;
        }

        // Use html2pdf library (need to add CDN)
        if (typeof html2pdf === 'undefined') {
            console.error('html2pdf library not loaded');
            alert('La bibliothèque PDF n\'est pas chargée. Veuillez recharger la page.');
            return;
        }

        const opt = {
            margin: [10, 10, 10, 10],
            filename: filename || 'dashboard-savora.pdf',
            image: { type: 'jpeg', quality: 0.98 },
            html2canvas: { scale: 2, useCORS: true },
            jsPDF: { unit: 'mm', format: 'a4', orientation: 'landscape' }
        };

        html2pdf().set(opt).from(element).save();
    },

    // Export dashboard data to Excel (CSV format)
    exportToExcel: function(data, filename) {
        if (!data || !Array.isArray(data)) {
            console.error('Invalid data for Excel export');
            return;
        }

        // Convert data to CSV
        const headers = Object.keys(data[0] || {});
        const csvContent = [
            headers.join(','),
            ...data.map(row => 
                headers.map(header => {
                    const value = row[header];
                    // Escape commas and quotes
                    if (typeof value === 'string' && (value.includes(',') || value.includes('"'))) {
                        return `"${value.replace(/"/g, '""')}"`;
                    }
                    return value ?? '';
                }).join(',')
            )
        ].join('\n');

        // Create blob and download
        const blob = new Blob(['\ufeff' + csvContent], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        const url = URL.createObjectURL(blob);
        
        link.setAttribute('href', url);
        link.setAttribute('download', filename || 'dashboard-savora.csv');
        link.style.visibility = 'hidden';
        
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    },

    // Export chart as image
    exportChartAsImage: function(canvasId, filename) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error('Chart canvas not found');
            return;
        }

        const url = canvas.toDataURL('image/png');
        const link = document.createElement('a');
        link.download = filename || `${canvasId}.png`;
        link.href = url;
        link.click();
    }
};

// Download file from base64 string
window.downloadFile = function(base64String, filename, contentType) {
    try {
        // Convert base64 to binary
        const binaryString = atob(base64String);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        
        // Create blob and download
        const blob = new Blob([bytes], { type: contentType || 'application/pdf' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    } catch (error) {
        console.error('Error downloading file:', error);
        alert('Erreur lors du téléchargement du fichier');
    }
};

