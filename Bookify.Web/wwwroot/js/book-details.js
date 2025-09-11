function onAddCopySuccess(row) {
	ShowSuccessMessage()
	var modal = $('#Modal');
	modal.modal('hide');

	$('tbody').prepend(row);
	KTMenu.createInstances();

	var count = $('#copiesCount');
	var newCount = parseInt(count.text()) + 1;
	count.text(newCount);

	if (newCount === 1) {
		$('.alert').addClass("d-none");
		$('table').removeClass("d-none");
	}
}

function onEditCopySuccess(row) {
	ShowSuccessMessage()
	var modal = $('#Modal');
	modal.modal('hide');

	$(updatedRow).replaceWith(row);

	KTMenu.createInstances();
}