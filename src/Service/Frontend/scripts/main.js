var FC = (function (FC, window, $) {

    var ZOOM_IN_DELTA = 0.33,
        ZOOM_OUT_DELTA = 3;

    // TODO: Clean the main script.
    $(function () {

        FC.isLeftButtonDown = false;
        $(document).on("mouseup", function (e) {
            if (e.which == 1)
                FC.isLeftButtonDown = false;
        });

        FC.Map.init();        

        function setActivePage(page) {
            $(".fc2-menu-icon.active").removeClass("active");
            $(".fc2-menu-icon." + page).addClass("active");
            $("section:visible").hide();
            $("section." + page).show();

            if (page === "geography") {
                $(".OverlaysBR").css("bottom", "7px");
            }

            if (page === "results") 
                FC.Results.updateSection();
            if (page === "export")
                FC.Export.update();

            $(window).resize();
        }

        FC.setActivePage = function (page) {
            setActivePage(page);
            FC.state.setActivePage(page);
        }

        FC.state = new FC.ClientState({
            url: FC.Settings.FETCHCLIMATE_2_SERVICE_URL,
            getConfiguration: FC.getConfiguration,
            performRequest: function (spatial, temporal) {
                var request = new FC.Request({
                    spatial: spatial,
                    temporal: temporal
                });
                return request.perform();
            }
        });

        FC.state.refreshConfiguration().then(
            function (config) {
                FC.Geography.initialize();
                FC.Layers.initialize();
                FC.Time.initialize();
                FC.Results.initialize();
                FC.Export.initialize();
                if (FC.mapEntities) FC.Map.updateGeoPanel();

                $(".fc2-menu-icon").click(function () {
                    var page = $(this).attr("class").split(" ")[1];
                    FC.setActivePage(page);
                });

                setActivePage(FC.state.activePage);
            },
            function (error) {
                $(".connection-failed-msg").show();
            });

        FC.state.on("statechange", function (prop) {
            if (prop !== "activePage") return;
            FC.Map.updateGeoPanel();
        });

        // NOTE: DEBUG ONLY.
        // FC.state.on("statuschange", function (status) {
        //     console.log("[state] status:", status);
        // });

        // FC.state.on("statechange", function (property) {
        //     console.log("statechange", property);
        // });

        // FC.state.on("hashchange", function () {
        //     console.log("hashchange");
        // });

        // FC.state.on("windowhashchange", function () {
        //     console.log("windowhashchange");
        // });
    });

    return FC;
}(FC || {}, window, jQuery));