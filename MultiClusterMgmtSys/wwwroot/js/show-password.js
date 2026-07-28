const passwordTimeouts = new WeakMap();

function showPassword(inputElement) {
    if (!inputElement) return;
    const existingTimeout = passwordTimeouts.get(inputElement);
    if (existingTimeout) clearTimeout(existingTimeout);
    if (inputElement.type === 'password') {
        inputElement.type = 'text';
        const timeoutId = setTimeout(function () {
            inputElement.type = 'password';
            passwordTimeouts.delete(inputElement);
        }, 5000);
        passwordTimeouts.set(inputElement, timeoutId);
    } else {
        inputElement.type = 'password';
        passwordTimeouts.delete(inputElement);
    }
}