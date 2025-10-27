document.addEventListener('shown.bs.modal', (e) => {
    if (e.target.id !== 'editTransactionModal') return;
    const checkbox = e.target.querySelector('#isRecurringCheckbox');
    const select = e.target.querySelector('#recurrenceFrequency');
    if (!checkbox || !select) return;

    select.style.display = checkbox.checked ? 'inline-block' : 'none';
    checkbox.addEventListener('change', () => {
        select.style.display = checkbox.checked ? 'inline-block' : 'none';
    });
});

