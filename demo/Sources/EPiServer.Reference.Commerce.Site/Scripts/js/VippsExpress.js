var VippsExpress = {
    init: function () {
        $(document).on('submit', '.vipps-express-form', VippsExpress.checkout);
    },

    checkout: function (e) {
        e.preventDefault();
        var form = $(this).closest("form");
        var data = form.serialize();
        $.ajax({
            type: form[0].method,
            url: form[0].action + '?' + data,
            success: function (response) {
                if (response.RedirectUrl)
                    document.location = response.RedirectUrl;
                else
                    $(form).find('.vipps-express-error').text(response.ErrorMessage);
            },
            error: function(jqXhr) {
                $(form).find('.vipps-express-error').text(jqXhr.responseJSON.Message);
            }
        });
    },

};