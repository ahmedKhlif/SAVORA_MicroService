// SAVORA Calendar Module
// Handles FullCalendar initialization and interactions

let savoraCalendar = null;
let dotNetRef = null;

// Initialize the calendar
window.initSavoraCalendar = function (elementId, events, dotNetReference, options) {
    dotNetRef = dotNetReference;
    
    const calendarEl = document.getElementById(elementId);
    if (!calendarEl) {
        console.error('Calendar element not found:', elementId);
        return;
    }

    // Destroy existing calendar if any
    if (savoraCalendar) {
        savoraCalendar.destroy();
    }

    // Default options
    const defaultOptions = {
        editable: true,
        selectable: true,
        ...options
    };

    savoraCalendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        locale: 'fr',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay,listWeek'
        },
        buttonText: {
            today: "Aujourd'hui",
            month: 'Mois',
            week: 'Semaine',
            day: 'Jour',
            list: 'Liste'
        },
        events: events,
        editable: defaultOptions.editable,
        eventEditable: defaultOptions.editable, // Allow events to be dragged
        eventStartEditable: defaultOptions.editable, // Allow start time to be changed
        eventDurationEditable: false, // Don't allow duration changes for now
        selectable: defaultOptions.selectable,
        selectMirror: true,
        dayMaxEvents: 3,
        weekends: true,
        nowIndicator: true,
        eventOverlap: true, // Allow events to overlap
        eventTimeFormat: {
            hour: '2-digit',
            minute: '2-digit',
            hour12: false
        },
        slotMinTime: '07:00:00',
        slotMaxTime: '20:00:00',
        allDaySlot: false,
        height: 'auto',
        aspectRatio: 1.8,
        
        // Custom event rendering
        eventDidMount: function(info) {
            // Add tooltip
            const tooltip = document.createElement('div');
            tooltip.className = 'calendar-tooltip';
            tooltip.innerHTML = `
                <strong>${info.event.title}</strong>
                <br><small>${info.event.extendedProps.clientName || ''}</small>
                <br><small>${info.event.extendedProps.technicianName || 'Non assigné'}</small>
            `;
            
            info.el.setAttribute('data-bs-toggle', 'tooltip');
            info.el.setAttribute('data-bs-html', 'true');
            info.el.setAttribute('title', tooltip.innerHTML);
            
            // Initialize Bootstrap tooltip
            new bootstrap.Tooltip(info.el, {
                html: true,
                placement: 'top',
                container: 'body'
            });
        },
        
        // Click on event
        eventClick: function(info) {
            info.jsEvent.preventDefault();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnEventClick', info.event.id);
            }
        },
        
        // Click on date
        dateClick: function(info) {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnDateClick', info.dateStr);
            }
        },
        
        // Drag and drop event
        eventDrop: function(info) {
            if (dotNetRef) {
                // Call C# method to save the new date
                dotNetRef.invokeMethodAsync('OnEventDrop', info.event.id, info.event.startStr)
                    .catch(function(error) {
                        console.error('Error dropping event:', error);
                        // Revert the event position on error
                        info.revert();
                    });
            }
        },
        
        // Event resize (for time-based events)
        eventResize: function(info) {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnEventResize', info.event.id, info.event.startStr, info.event.endStr)
                    .catch(function(error) {
                        console.error('Error resizing event:', error);
                        info.revert();
                    });
            }
        },
        
        // Before event is dragged
        eventDragStart: function(info) {
            // Add visual feedback
            info.el.style.opacity = '0.7';
            info.el.style.cursor = 'grabbing';
        },
        
        // After drag ends (whether successful or not)
        eventDragStop: function(info) {
            // Restore visual state
            info.el.style.opacity = '1';
            info.el.style.cursor = 'move';
        },
        
        // Select date range
        select: function(info) {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnDateRangeSelect', info.startStr, info.endStr);
            }
        },
        
        // Loading indicator
        loading: function(isLoading) {
            const loader = document.getElementById('calendar-loader');
            if (loader) {
                loader.style.display = isLoading ? 'flex' : 'none';
            }
        }
    });

    savoraCalendar.render();
    
    // Re-init feather icons
    if (typeof feather !== 'undefined') {
        feather.replace();
    }
    
    return true;
};

// Update calendar events
window.updateCalendarEvents = function (events) {
    if (savoraCalendar) {
        // Remove all events
        savoraCalendar.removeAllEvents();
        // Add new events
        savoraCalendar.addEventSource(events);
    }
};

// Add single event
window.addCalendarEvent = function (event) {
    if (savoraCalendar) {
        savoraCalendar.addEvent(event);
    }
};

// Remove event by ID
window.removeCalendarEvent = function (eventId) {
    if (savoraCalendar) {
        const event = savoraCalendar.getEventById(eventId);
        if (event) {
            event.remove();
        }
    }
};

// Navigate calendar
window.calendarNavigate = function (action) {
    if (savoraCalendar) {
        switch (action) {
            case 'prev':
                savoraCalendar.prev();
                break;
            case 'next':
                savoraCalendar.next();
                break;
            case 'today':
                savoraCalendar.today();
                break;
        }
    }
};

// Change calendar view
window.calendarChangeView = function (viewName) {
    if (savoraCalendar) {
        savoraCalendar.changeView(viewName);
    }
};

// Go to specific date
window.calendarGoToDate = function (dateStr) {
    if (savoraCalendar) {
        savoraCalendar.gotoDate(dateStr);
    }
};

// Get current calendar date
window.getCalendarCurrentDate = function () {
    if (savoraCalendar) {
        return savoraCalendar.getDate().toISOString();
    }
    return null;
};

// Destroy calendar
window.destroySavoraCalendar = function () {
    if (savoraCalendar) {
        savoraCalendar.destroy();
        savoraCalendar = null;
    }
};

// Refresh calendar size (useful after container resize)
window.refreshCalendarSize = function () {
    if (savoraCalendar) {
        savoraCalendar.updateSize();
    }
};

