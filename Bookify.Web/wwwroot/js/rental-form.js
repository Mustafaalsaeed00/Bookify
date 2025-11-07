var currentCopies = [];
var selectedCopies = [];
var isEditMode = false;

$(document).ready(function () {

    if ($('.js-copy').length > 0) {
        prepareInputs();
        currentCopies = selectedCopies;
        isEditMode = true;
    }


    $('.js-search').on('click', function (e) {
        e.preventDefault();

        var serial = $("#Value").val();
        if (selectedCopies.find(c => c.serial == serial)) {
            ShowErrorMessage('You cannot add the same copy')
            return;
        }

        if ($('.js-copy').length >= maxAllowedCopies) {
            ShowErrorMessage(`You cannot add more than ${maxAllowedCopies} book(s)`)
            return;
        }

       

        $("#SearchForm").submit();
    });


    $('body').delegate('.js-remove', 'click', function (e) {
        var btn = $(this);
        var container = btn.parents('.js-copy-container');

        if (isEditMode) {
            console.log('entered')
            btn.toggleClass('btn-light-danger btn-light-success js-remove js-readd').text('Re-Add');
            container.find('img').css('opacity', '0.5');
            container.find('h4').css('text-decoration', 'line-through');
            container.find('.js-copy').toggleClass('js-copy js-removed').removeAttr('name').removeAttr('id');
        } else {
            container.remove();
        }
       
        prepareInputs();

        console.log(selectedCopies.length);

        if ($.isEmptyObject(selectedCopies) || JSON.stringify(currentCopies) == JSON.stringify(selectedCopies))
            $('#CopiesForm').find(':submit').addClass('d-none');
        else
            $('#CopiesForm').find(':submit').removeClass('d-none');

    });

    $('body').delegate('.js-readd', 'click', function (e) {
        var btn = $(this);
        var container = btn.parents('.js-copy-container');

        btn.toggleClass('btn-light-danger btn-light-success js-remove js-readd').text('Remove');
        container.find('img').css('opacity', '1');
        container.find('h4').css('text-decoration', 'none');
        container.find('.js-removed').toggleClass('js-copy js-removed');
        
        prepareInputs();

        if (JSON.stringify(currentCopies) == JSON.stringify(selectedCopies))
            $('#CopiesForm').find(':submit').addClass('d-none');
        else
            $('#CopiesForm').find(':submit').removeClass('d-none');

    });


});

function onAddCopySuccess(copy) {
    $('#Value').val('');

    var bookId = $(copy).find('.js-copy').data('book-id');

    if (selectedCopies.find(c => c.bookId == bookId)) {
        ShowErrorMessage(`You cannot add more than one copy for the same book`);
        return;
    }

    $('#CopiesForm').prepend(copy);
    $('#CopiesForm').find(':submit').removeClass('d-none');

    prepareInputs();
}

function prepareInputs() {
    var copies = $('.js-copy');

    selectedCopies = [];

    $.each(copies, function (i, input) {
        var $input = $(input);
        selectedCopies.push({ serial: $input.val(), bookId: $input.data('book-id') });
        $input.attr('name', `SelectedCopies[${i}]`).attr('id', `SelectedCopies_${i}_`);
    });
}