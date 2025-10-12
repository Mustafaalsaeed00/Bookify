$(document).ready(function () {
    $('#Governorate').on('change', function () {
        var governorateId = $(this).val();
        var areaElm = $('#Area');
        areaElm.empty();
        areaElm.append('<option></option>');
        if (governorateId != '') {
            $.ajax({
                url: "/Subscribers/GetAreas?governorateId=" + governorateId,
                success: function (areas) {
                    $.each(areas, function (i, area) {
                        var item = $('<option></option>').attr('value', area.value).text(area.text);
                        areaElm.append(item);
                    });
                },
                error: function () {
                    ShowErrorMessage();
                }

            });
        }
    });
});