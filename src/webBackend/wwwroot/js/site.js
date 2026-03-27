// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


function generalDelete(entityName, id, displayName) {
    Swal.fire({
        title: 'Emin misiniz?',
        text: `'${displayName}' kalıcı olarak silinecek!`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Evet, Sil!',
        cancelButtonText: 'Vazgeç',
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {
            var form = document.createElement("form");
            form.setAttribute("method", "post");
           
            form.setAttribute("action", "/" + entityName + "/Delete/" + id);

            var token = $('input[name="__RequestVerificationToken"]').val();
            var tokenInput = document.createElement("input");
            tokenInput.setAttribute("type", "hidden");
            tokenInput.setAttribute("name", "__RequestVerificationToken");
            tokenInput.setAttribute("value", token);

            form.appendChild(tokenInput);
            document.body.appendChild(form);
            form.submit();
        }
    });
}
