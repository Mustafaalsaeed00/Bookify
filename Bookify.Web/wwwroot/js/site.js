
function ShowSuccessMessage(message = "Saved Successfully!") {
    Swal.fire({
        icon: "success",
        title: "Success",
        text: message,
        customClass: {
            confirmButton: "btn btn-outline btn-outline-dashed btn-outline-primary btn-active-light-primary"
        }

    });
}

function ShowErrorMessage(message = "Something went wrong!") {
    Swal.fire({
        icon: "error",
        title: "Oops...",
        text: message,
        customClass: {
            confirmButton: "btn btn-outline btn-outline-dashed btn-outline-primary btn-active-light-primary"
        }

    });
}


$(document).ready(function () {
    var message = $('#Message').text();
    if (message !== '') {
        ShowSuccessMessage(message)
    }
});