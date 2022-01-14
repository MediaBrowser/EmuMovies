define(['loading', 'emby-input', 'emby-button', 'emby-checkbox'], function (loading) {
    'use strict';

    function loadPage(page, config) {

        page.querySelector('.txtUsername').value = config.EmuMoviesUsername || '';
        page.querySelector('.txtPassword').value = config.EmuMoviesPassword || '';

        loading.hide();
    }

    function onSubmit(e) {

        e.preventDefault();

        loading.show();

        var form = this;

        getConfig().then(function (config) {

            config.EmuMoviesUsername = form.querySelector('.txtUsername').value;
            config.EmuMoviesPassword = form.querySelector('.txtPassword').value;

            ApiClient.updateNamedConfiguration("emumovies", config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    function getConfig() {

        return ApiClient.getNamedConfiguration("emumovies");
    }

    return function (view, params) {

        view.querySelector('form').addEventListener('submit', onSubmit);

        view.addEventListener('viewshow', function () {

            loading.show();

            var page = this;

            getConfig().then(function (response) {

                loadPage(page, response);
            });
        });
    };

});
