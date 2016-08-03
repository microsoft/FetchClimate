(function (FC, $) {
    (function (Export) {
        "use strict";

        var _$page,
            _exportPanel;

        Export.initialize = function () {
          _$page = $("section.export");
          _exportPanel = new FC.Controls.ExportPanel($(".export-panel"));
        };

        Export.update = function () {
            _exportPanel.update();
        }

    })(FC.Export || (FC.Export = {}));
})(window.FC = window.FC || {}, jQuery);