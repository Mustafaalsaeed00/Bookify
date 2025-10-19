$(document).ready(function () {
	$('.js-renew').on('click', function () {
		var subscriberKey = $(this).data('key');
		bootbox.confirm({
			message: 'Are you sure that you need to renew this subscription?',
			centerVertical: true,
			buttons: {
				confirm: {
					label: 'Yes',
					className: 'btn-sm btn-success'
				},
				cancel: {
					label: 'No',
					className: 'btn-sm btn-secondary'
				}
			},
			callback: function (result) {
				if (result) {
					$.post({
						url: `/Subscribers/RenewSubscription?sKey=${subscriberKey}`,
						data: {
							"__RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
						},
						success: function (row) {
							$('#SubscriptionsTable').find('tbody').append(row);
							var activeIcon = $("#ActiveStatusIcon");
							activeIcon.removeClass('d-none');
							activeIcon.siblings('span').remove();
							activeIcon.parents('.card').removeClass('bg-warning').addClass('bg-success');

							$('#CardStatus').text('Active Subscriber');

							$("#StatusBadge").removeClass('badge-light-warning').addClass('badge-light-success').text('Active Subscriber');

							ShowSuccessMessage();

						},
						error: function () {
							ShowErrorMessage();
						}
					});
				}
			}
		});
    });
});