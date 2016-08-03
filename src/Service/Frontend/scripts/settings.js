(function (FC, undefined) {
    FC.Settings = {
        BING_MAPS_API_KEY: "AvR0E-CdUXzV9xQwQe_bxBOsz2BFM0uwn1bQwzCV44uNajThw-9-TQzpRM7GL5FH",
        BING_MAPS_LOCATIONS_SEARCH_URL: "http://dev.virtualearth.net/REST/v1/Locations",
        MAP_SEARCH_DELAY: 50, // ms

        DISPLAY_PRECISION: 3, // Digits after decimal point when showing lat/lon
        STORE_PRECISION: 6, // Digits after decimal point when reading/storing input data

        NAVIGATION_PANEL_WIDTH: 40, // Pixels, also defined in _variables.less
        SIDE_PANEL_WIDTH: 300, // Pixels, also defined in _variables.less
        TIME_SLICE_PANEL_PADDING: 20, // Pixels, also defined in _variables.less

        DEFAULT_OPACITY: 75,
        DEFAULT_PALETTE: "#ffffffff,#ff3c00ff", // Default color palette for heatmaps
        DEFAULT_PALETTE_BANDS: 10, // Number of bands for discrete palette
        PALETTE_HEIGHT: 16, // Height of palette in pixels
        PALETTE_WIDTH: 234, // Width of palette in pixels
        DEFAULT_MARKER_SIZE: 25, // Default size of marker on results section
        DEFAULT_MARKER_SHAPE: "circle", // Default shape of marker on results section
        DEFAULT_MARKER_BORDER: "black", // Default border color of marker on results section
        DEFAULT_NAN_VALUE_COLOR: {
            r: 200,
            g: 200,
            b: 200,
            a: 0.3
        },
        GEOGRAPHY_TILE_SLIDE_TIME: 400, // Time in milliseconds of geography area tile sliding animation

        SLIDER_SMOOTHNESS: 100,

        AREA_PLOT_BACKGROUND_COLOR: "rgba(100,100,100,0.5)",
        AREA_PLOT_FOREGROUND_COLOR: "rgba(255,0,0,0.5)",
        HOVERED_POINT_POLYLINE_COLOR: "rgba(0,255,0,0.5)",
        PROBE_POINT_POLYLINE_COLOR: "rgba(0,0,255,0.5)",

        GEOGRAPHY_EMPTY_MESSAGE: "Add region(s) and/or point(s) by first selecting one of the map drawing tools above.",
        NO_LAYERS_SELECTED_MESSAGE: "There are no layers selected.Please select one or more layers in the <a href=\"javascript:FC.setActivePage(\'layers\')\">layers section</a>.",
        LAYERS_EMPTY_MESSAGE: "Choose data layers of interest from the list on the right.",
        NO_AREA_SELECTED_MESSAGE: "There are no areas of interest selected.Please draw one or more regions or points in the <a href=\"javascript:FC.setActivePage(\'geography\')\">geography section</a>.",
        NO_DATA_AVAILABLE_MESSAGE: "There are no data available yet.Please wait until data is shown in the <a href=\"javascript:FC.setActivePage(\'results\')\">results section</a> and come back.",
        NO_DATASOURCES_SELECTED_MESSAGE: "There are no data sources selected for this layer. Layer contents will not appear in the results",

        LAYERS_BY_CATEGORY_MESSAGE: "Layers by category",
        LAYERS_BY_NAME_MESSAGE: "Layers by name",

        RESET_YEARS_TITLE: "Reset years selection to the default",
        RESET_DAYS_TITLE: "Reset days selection to the entire year",
        RESET_HOURS_TITLE: "Reset hours selection to the entire day",

        EDIT_YEARS_TITLE: "Switch to the years selection mode",
        EDIT_DAYS_TITLE: "Switch to the days selection mode",
        EDIT_HOURS_TITLE: "Switch to the hours selection mode",

        DEFAULT_EMAIL_SUBJECT: "Look at this FetchClimate data",

        FETCHCLIMATE_2_SERVICE_URL: "",
    };
})(window.FC = window.FC || {});