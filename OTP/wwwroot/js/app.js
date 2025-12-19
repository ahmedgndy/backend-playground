// =============================================================================
// OTP AUTHENTICATION DEMO - FRONTEND JAVASCRIPT
// =============================================================================
// This file handles all frontend interactions for the OTP flow:
// - Sending OTP request to backend
// - Handling OTP input (auto-focus, paste support)
// - Verifying OTP
// - Timer countdown
// - Toast notifications
// =============================================================================

// API Base URL - adjust if your backend runs on a different port
const API_BASE_URL = '';

// =============================================================================
// STATE MANAGEMENT
// =============================================================================
const state = {
    email: '',
    timerInterval: null,
    timeRemaining: 600, // 10 minutes in seconds
    resendCooldown: 60,  // Resend cooldown in seconds
    resendTimer: null
};

// =============================================================================
// DOM ELEMENTS
// =============================================================================
const elements = {
    // Step indicators
    step1Indicator: document.getElementById('step-1-indicator'),
    step2Indicator: document.getElementById('step-2-indicator'),
    stepLine: document.getElementById('step-line'),

    // Form steps
    step1: document.getElementById('step-1'),
    step2: document.getElementById('step-2'),
    step3: document.getElementById('step-3'),

    // Forms
    emailForm: document.getElementById('email-form'),
    otpForm: document.getElementById('otp-form'),

    // Inputs
    emailInput: document.getElementById('email'),
    otpInputs: document.querySelectorAll('.otp-input'),

    // Buttons
    sendOtpBtn: document.getElementById('send-otp-btn'),
    verifyOtpBtn: document.getElementById('verify-otp-btn'),
    resendBtn: document.getElementById('resend-btn'),
    changeEmailBtn: document.getElementById('change-email-btn'),
    startOverBtn: document.getElementById('start-over-btn'),

    // Display elements
    displayEmail: document.getElementById('display-email'),
    timer: document.getElementById('timer'),
    timerText: document.getElementById('timer-text'),

    // Toast container
    toastContainer: document.getElementById('toast-container')
};

// =============================================================================
// EVENT LISTENERS
// =============================================================================
document.addEventListener('DOMContentLoaded', () => {
    // Email form submission
    elements.emailForm.addEventListener('submit', handleEmailSubmit);

    // OTP form submission
    elements.otpForm.addEventListener('submit', handleOtpSubmit);

    // OTP input handling
    setupOtpInputs();

    // Button handlers
    elements.resendBtn.addEventListener('click', handleResendOtp);
    elements.changeEmailBtn.addEventListener('click', handleChangeEmail);
    elements.startOverBtn.addEventListener('click', handleStartOver);
});

// =============================================================================
// EMAIL SUBMISSION HANDLER
// =============================================================================
async function handleEmailSubmit(e) {
    e.preventDefault();

    const email = elements.emailInput.value.trim();

    if (!validateEmail(email)) {
        showToast('error', 'Invalid Email', 'Please enter a valid email address');
        return;
    }

    // Store email in state
    state.email = email;

    // Show loading state
    setButtonLoading(elements.sendOtpBtn, true);

    try {
        // Send OTP request to backend
        const response = await fetch(`${API_BASE_URL}/api/auth/request-otp`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email })
        });

        const data = await response.json();

        if (response.ok) {
            showToast('success', 'Code Sent!', 'Check your email for the verification code');

            // Update display
            elements.displayEmail.textContent = email;

            // Move to step 2
            goToStep(2);

            // Start timer
            startTimer();

            // Start resend cooldown
            startResendCooldown();

            // Focus first OTP input
            elements.otpInputs[0].focus();
        } else {
            // Handle rate limiting
            if (response.status === 429) {
                showToast('error', 'Too Many Requests', 'Please wait a moment before trying again');
            } else {
                showToast('error', 'Error', data.message || 'Failed to send verification code');
            }
        }
    } catch (error) {
        console.error('Error sending OTP:', error);
        showToast('error', 'Connection Error', 'Could not connect to the server');
    } finally {
        setButtonLoading(elements.sendOtpBtn, false);
    }
}

// =============================================================================
// OTP VERIFICATION HANDLER
// =============================================================================
async function handleOtpSubmit(e) {
    e.preventDefault();

    // Get OTP from inputs
    const otp = getOtpValue();

    if (otp.length !== 6) {
        showToast('error', 'Invalid Code', 'Please enter all 6 digits');
        shakeOtpInputs();
        return;
    }

    // Show loading state
    setButtonLoading(elements.verifyOtpBtn, true);

    try {
        // Verify OTP with backend
        const response = await fetch(`${API_BASE_URL}/api/auth/verify-otp`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                email: state.email,
                otp: otp
            })
        });

        const data = await response.json();

        if (response.ok && data.success) {
            showToast('success', 'Verified!', 'Your email has been verified successfully');

            // Stop timer
            stopTimer();

            // Move to success step
            goToStep(3);
        } else {
            // Handle rate limiting
            if (response.status === 429) {
                showToast('error', 'Too Many Attempts', 'Please wait a moment before trying again');
            } else {
                showToast('error', 'Verification Failed', data.message || 'Invalid or expired code');
            }
            shakeOtpInputs();
            clearOtpInputs();
        }
    } catch (error) {
        console.error('Error verifying OTP:', error);
        showToast('error', 'Connection Error', 'Could not connect to the server');
    } finally {
        setButtonLoading(elements.verifyOtpBtn, false);
    }
}

// =============================================================================
// OTP INPUT HANDLING
// =============================================================================
function setupOtpInputs() {
    elements.otpInputs.forEach((input, index) => {
        // Handle input
        input.addEventListener('input', (e) => {
            const value = e.target.value;

            // Only allow digits
            if (!/^\d*$/.test(value)) {
                e.target.value = '';
                return;
            }

            // Update filled state
            updateOtpInputStates();

            // Move to next input
            if (value && index < 5) {
                elements.otpInputs[index + 1].focus();
            }

            // Auto-submit if all filled
            if (getOtpValue().length === 6) {
                elements.verifyOtpBtn.focus();
            }
        });

        // Handle keydown for backspace
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Backspace' && !e.target.value && index > 0) {
                elements.otpInputs[index - 1].focus();
            }
        });

        // Handle paste
        input.addEventListener('paste', (e) => {
            e.preventDefault();
            const pastedData = e.clipboardData.getData('text').trim();

            // Only handle if it looks like an OTP
            if (/^\d{6}$/.test(pastedData)) {
                pastedData.split('').forEach((digit, i) => {
                    if (elements.otpInputs[i]) {
                        elements.otpInputs[i].value = digit;
                    }
                });
                updateOtpInputStates();
                elements.otpInputs[5].focus();
            }
        });

        // Handle focus
        input.addEventListener('focus', () => {
            input.select();
        });
    });
}

function getOtpValue() {
    return Array.from(elements.otpInputs).map(input => input.value).join('');
}

function clearOtpInputs() {
    elements.otpInputs.forEach(input => {
        input.value = '';
        input.classList.remove('filled', 'error');
    });
    elements.otpInputs[0].focus();
}

function updateOtpInputStates() {
    elements.otpInputs.forEach(input => {
        input.classList.remove('error');
        if (input.value) {
            input.classList.add('filled');
        } else {
            input.classList.remove('filled');
        }
    });
}

function shakeOtpInputs() {
    elements.otpInputs.forEach(input => {
        input.classList.add('error');
    });
}

// =============================================================================
// TIMER FUNCTIONS
// =============================================================================
function startTimer() {
    state.timeRemaining = 600; // 10 minutes
    updateTimerDisplay();

    state.timerInterval = setInterval(() => {
        state.timeRemaining--;
        updateTimerDisplay();

        if (state.timeRemaining <= 0) {
            stopTimer();
            elements.timerText.textContent = 'Code has expired.';
            elements.timer.textContent = '';
            elements.timer.classList.add('expired');
            showToast('warning', 'Code Expired', 'Please request a new verification code');
        }
    }, 1000);
}

function stopTimer() {
    if (state.timerInterval) {
        clearInterval(state.timerInterval);
        state.timerInterval = null;
    }
}

function updateTimerDisplay() {
    const minutes = Math.floor(state.timeRemaining / 60);
    const seconds = state.timeRemaining % 60;

    elements.timer.textContent = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;

    // Add warning class when low on time
    if (state.timeRemaining <= 60) {
        elements.timer.classList.add('warning');
    } else {
        elements.timer.classList.remove('warning');
    }
}

// =============================================================================
// RESEND COOLDOWN
// =============================================================================
function startResendCooldown() {
    let cooldown = state.resendCooldown;
    elements.resendBtn.disabled = true;
    elements.resendBtn.textContent = `Resend in ${cooldown}s`;

    state.resendTimer = setInterval(() => {
        cooldown--;

        if (cooldown <= 0) {
            clearInterval(state.resendTimer);
            elements.resendBtn.disabled = false;
            elements.resendBtn.textContent = 'Resend Code';
        } else {
            elements.resendBtn.textContent = `Resend in ${cooldown}s`;
        }
    }, 1000);
}

// =============================================================================
// BUTTON HANDLERS
// =============================================================================
async function handleResendOtp() {
    // Same as email submit but without changing steps
    setButtonLoading(elements.resendBtn, true);

    try {
        const response = await fetch(`${API_BASE_URL}/api/auth/request-otp`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email: state.email })
        });

        if (response.ok) {
            showToast('success', 'Code Resent!', 'A new verification code has been sent');

            // Restart timer
            stopTimer();
            startTimer();

            // Restart cooldown
            startResendCooldown();

            // Clear and focus inputs
            clearOtpInputs();
        } else if (response.status === 429) {
            showToast('error', 'Too Many Requests', 'Please wait before requesting another code');
        } else {
            const data = await response.json();
            showToast('error', 'Error', data.message || 'Failed to resend code');
        }
    } catch (error) {
        console.error('Error resending OTP:', error);
        showToast('error', 'Connection Error', 'Could not connect to the server');
    } finally {
        setButtonLoading(elements.resendBtn, false);
    }
}

function handleChangeEmail() {
    stopTimer();
    if (state.resendTimer) {
        clearInterval(state.resendTimer);
    }
    goToStep(1);
    elements.emailInput.focus();
}

function handleStartOver() {
    // Reset everything
    state.email = '';
    elements.emailInput.value = '';
    clearOtpInputs();

    elements.timer.classList.remove('warning', 'expired');
    elements.timerText.textContent = 'Code expires in';

    goToStep(1);
}

// =============================================================================
// STEP NAVIGATION
// =============================================================================
function goToStep(stepNumber) {
    // Hide all steps
    elements.step1.classList.remove('active');
    elements.step2.classList.remove('active');
    elements.step3.classList.remove('active');

    // Update indicators
    elements.step1Indicator.classList.remove('active', 'completed');
    elements.step2Indicator.classList.remove('active', 'completed');
    elements.stepLine.classList.remove('active');

    // Show requested step
    switch (stepNumber) {
        case 1:
            elements.step1.classList.add('active');
            elements.step1Indicator.classList.add('active');
            break;
        case 2:
            elements.step2.classList.add('active');
            elements.step1Indicator.classList.add('completed');
            elements.step2Indicator.classList.add('active');
            elements.stepLine.classList.add('active');
            break;
        case 3:
            elements.step3.classList.add('active');
            elements.step1Indicator.classList.add('completed');
            elements.step2Indicator.classList.add('completed');
            elements.stepLine.classList.add('active');
            break;
    }
}

// =============================================================================
// UTILITY FUNCTIONS
// =============================================================================
function validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

function setButtonLoading(button, isLoading) {
    if (isLoading) {
        button.classList.add('loading');
        button.disabled = true;
    } else {
        button.classList.remove('loading');
        button.disabled = false;
    }
}

// =============================================================================
// TOAST NOTIFICATIONS
// =============================================================================
function showToast(type, title, message) {
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;

    // Icon based on type
    let iconPath = '';
    switch (type) {
        case 'success':
            iconPath = 'M22 11.08V12a10 10 0 1 1-5.93-9.14M22 4L12 14.01l-3-3';
            break;
        case 'error':
            iconPath = 'M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20zm0 5v6m0 4h.01';
            break;
        case 'warning':
            iconPath = 'M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0zM12 9v4m0 4h.01';
            break;
        default:
            iconPath = 'M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20zm1 5v6h-2V7h2zm0 10h-2v-2h2v2z';
    }

    toast.innerHTML = `
        <svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="${iconPath}"/>
        </svg>
        <div class="toast-content">
            <div class="toast-title">${title}</div>
            <div class="toast-message">${message}</div>
        </div>
        <button class="toast-close" onclick="this.parentElement.remove()">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M18 6L6 18M6 6l12 12"/>
            </svg>
        </button>
    `;

    elements.toastContainer.appendChild(toast);

    // Auto-remove after 5 seconds
    setTimeout(() => {
        toast.classList.add('hide');
        setTimeout(() => toast.remove(), 300);
    }, 5000);
}
