define(['baseView', 'loading', 'emby-input', 'emby-button', 'emby-checkbox', 'emby-scroller'], function (BaseView, loading) {
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

    function View(view, params) {
        BaseView.apply(this, arguments);

        view.querySelector('form').addEventListener('submit', onSubmit);
    }

    Object.assign(View.prototype, BaseView.prototype);

    View.prototype.onResume = function (options) {

        BaseView.prototype.onResume.apply(this, arguments);

        loading.show();

        var page = this.view;

        getConfig().then(function (response) {

            loadPage(page, response);
        });
    };

    return View;
});
