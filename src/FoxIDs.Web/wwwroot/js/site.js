$(document).ready(function () {
    $('.markdown-page h1, .markdown-page h2, .markdown-page h3, .markdown-page h4').each(function () {

        var id = $(this).attr('id');
        $(this).addClass('anchor-heading');
        var anchor = $('<a class="anchor-link" href="#' + id + '">#</a>');
        $(this).append(anchor);
    });
});