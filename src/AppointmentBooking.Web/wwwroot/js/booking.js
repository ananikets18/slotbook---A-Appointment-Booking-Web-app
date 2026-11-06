/**
 * Interactive Slot Booking and Real-Time Double-Booking Prevention
 * Plain JavaScript (Vanilla JS - No React / No external framework)
 */

document.addEventListener('DOMContentLoaded', function () {
    const providerIdInput = document.getElementById('providerId');
    if (!providerIdInput) return; // Not on provider details page

    const providerId = providerIdInput.value;
    const datePicker = document.getElementById('appointmentDate');
    const slotGrid = document.getElementById('slotGridContainer');
    const slotLoading = document.getElementById('slotLoading');
    const noSlotsMessage = document.getElementById('noSlotsMessage');
    const selectedSlotIdInput = document.getElementById('selectedSlotId');
    const selectedServiceIdInput = document.getElementById('selectedServiceId');
    const bookingSummaryCard = document.getElementById('bookingSummaryCard');
    const summaryServiceName = document.getElementById('summaryServiceName');
    const summaryPrice = document.getElementById('summaryPrice');
    const summarySlotTime = document.getElementById('summarySlotTime');
    const confirmBookingBtn = document.getElementById('confirmBookingBtn');
    const conflictAlert = document.getElementById('conflictAlert');
    const conflictAlertMessage = document.getElementById('conflictAlertMessage');

    // Service Option Selection
    const serviceCards = document.querySelectorAll('.service-option-card');
    serviceCards.forEach(card => {
        card.addEventListener('click', function () {
            serviceCards.forEach(c => c.classList.remove('selected'));
            this.classList.add('selected');

            const serviceId = this.dataset.serviceId;
            const serviceName = this.dataset.serviceName;
            const servicePrice = this.dataset.servicePrice;

            selectedServiceIdInput.value = serviceId;
            if (summaryServiceName) summaryServiceName.textContent = serviceName;
            if (summaryPrice) summaryPrice.textContent = `₹${parseFloat(servicePrice).toLocaleString('en-IN')}`;

            updateBookingSummaryState();
        });
    });

    // Date Picker Change
    if (datePicker) {
        datePicker.addEventListener('change', function () {
            loadSlots(this.value);
        });
    }

    // Load available slots from API
    async function loadSlots(dateStr) {
        if (!dateStr) return;

        hideConflictAlert();
        selectedSlotIdInput.value = '';
        if (summarySlotTime) summarySlotTime.textContent = 'None selected';
        updateBookingSummaryState();

        if (slotLoading) slotLoading.style.display = 'block';
        if (noSlotsMessage) noSlotsMessage.style.display = 'none';
        if (slotGrid) slotGrid.innerHTML = '';

        try {
            // First fetch all slots to display the full 9am-5pm schedule, showing booked vs available
            const allResponse = await fetch(`/api/slots/all?providerId=${providerId}&date=${dateStr}`);
            const allSlots = await allResponse.json();

            if (slotLoading) slotLoading.style.display = 'none';

            if (!Array.isArray(allSlots) || allSlots.length === 0) {
                if (noSlotsMessage) noSlotsMessage.style.display = 'block';
                return;
            }

            renderSlots(allSlots);
        } catch (error) {
            console.error('Error fetching slots:', error);
            if (slotLoading) slotLoading.style.display = 'none';
            if (noSlotsMessage) {
                noSlotsMessage.textContent = 'Unable to load slots. Please try again.';
                noSlotsMessage.style.display = 'block';
            }
        }
    }

    function renderSlots(slots) {
        slotGrid.innerHTML = '';

        slots.forEach(slot => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'slot-btn';
            btn.dataset.slotId = slot.id;

            const startTimeFormatted = formatTime(slot.startTime);
            const endTimeFormatted = formatTime(slot.endTime);

            btn.textContent = `${startTimeFormatted} - ${endTimeFormatted}`;

            if (slot.isBooked) {
                btn.classList.add('booked');
                btn.disabled = true;
                btn.title = 'This slot has already been booked.';
            } else {
                btn.addEventListener('click', function () {
                    document.querySelectorAll('.slot-btn').forEach(b => b.classList.remove('selected'));
                    this.classList.add('selected');

                    selectedSlotIdInput.value = slot.id;
                    if (summarySlotTime) {
                        summarySlotTime.textContent = `${slot.date} at ${startTimeFormatted} - ${endTimeFormatted}`;
                    }
                    updateBookingSummaryState();
                });
            }

            slotGrid.appendChild(btn);
        });
    }

    function formatTime(timeStr) {
        if (!timeStr) return '';
        const parts = timeStr.split(':');
        const hour = parseInt(parts[0], 10);
        const min = parts[1] || '00';
        const ampm = hour >= 12 ? 'PM' : 'AM';
        const displayHour = hour % 12 === 0 ? 12 : hour % 12;
        return `${displayHour}:${min} ${ampm}`;
    }

    function updateBookingSummaryState() {
        const hasService = !!selectedServiceIdInput.value;
        const hasSlot = !!selectedSlotIdInput.value;

        if (confirmBookingBtn) {
            confirmBookingBtn.disabled = !(hasService && hasSlot);
        }
    }

    function showConflictAlert(message) {
        if (conflictAlert) {
            conflictAlertMessage.textContent = message;
            conflictAlert.style.display = 'block';
            conflictAlert.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }

    function hideConflictAlert() {
        if (conflictAlert) {
            conflictAlert.style.display = 'none';
        }
    }

    // Handle Confirm Booking Submission via AJAX
    const bookingForm = document.getElementById('bookingForm');
    if (bookingForm) {
        bookingForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const serviceId = selectedServiceIdInput.value;
            const slotId = selectedSlotIdInput.value;
            const customerNotes = document.getElementById('customerNotes')?.value || '';

            if (!serviceId || !slotId) {
                alert('Please select both a service and an available time slot.');
                return;
            }

            confirmBookingBtn.disabled = true;
            confirmBookingBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Securing Slot...';
            hideConflictAlert();

            try {
                const response = await fetch('/api/bookings', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        serviceId: serviceId,
                        slotId: slotId,
                        customerNotes: customerNotes
                    })
                });

                if (response.status === 401) {
                    window.location.href = `/Account/Login?returnUrl=${encodeURIComponent(window.location.pathname + window.location.search)}`;
                    return;
                }

                if (response.status === 409) {
                    const data = await response.json();
                    showConflictAlert(data.error || 'This slot was just booked by another user. Please choose a different slot.');
                    // Immediately re-fetch slots to reflect live state
                    await loadSlots(datePicker.value);
                    return;
                }

                if (response.ok) {
                    // Success! Redirect to My Bookings
                    window.location.href = '/Bookings';
                    return;
                }

                const errData = await response.json().catch(() => ({}));
                showConflictAlert(errData.error || 'Unable to complete booking. Please try again.');
            } catch (err) {
                console.error('Booking request failed:', err);
                showConflictAlert('Network or server error while placing booking. Please try again.');
            } finally {
                confirmBookingBtn.disabled = false;
                confirmBookingBtn.innerHTML = 'Confirm Appointment';
                updateBookingSummaryState();
            }
        });
    }

    // Initial slot load on page render
    if (datePicker && datePicker.value) {
        loadSlots(datePicker.value);
    }
});
