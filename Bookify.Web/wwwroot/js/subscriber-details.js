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

							$('#RentalButton').removeClass('d-none');

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

	$('.js-cancel-rental').on('click', function () {
		var btn = $(this);

		bootbox.confirm({
			message: 'Are you sure that you need to cancel this rental?',
			centerVertical: true,
			buttons: {
				confirm: {
					label: 'Yes',
					className: 'btn-sm btn-danger'
				},
				cancel: {
					label: 'No',
					className: 'btn-sm btn-secondary'
				}
			},
			callback: function (result) {
				if (result) {
					

					$.post({
						
						url: `/Rentals/MarkAsDeleted/${btn.data('id')}`,
						data: {
							"__RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
						},
						success: function (DeletedcopiesCount) {
							btn.parents('tr').remove();
							if ($('#RentalsTable tbody tr').length === 0) {
								$('#RentalsTable').fadeOut(function () {
									$('#Alert').fadeIn();
								});
							}
							let currentCount = parseInt($('#NumberOfRentals').text());
							console.log(currentCount);
							console.log(DeletedcopiesCount);

							$('#NumberOfRentals').text(currentCount - DeletedcopiesCount);
	
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