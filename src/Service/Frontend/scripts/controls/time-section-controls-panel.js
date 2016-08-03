(function (FC, $) {
    (function (Controls) {
        "use strict";

        Controls.TimeToggleBtnTile = function (source) {
            var self = this;

            self.base = Controls.Tile;
            self.base(source);

            var _$title = $("<div></div>", {
                class: "tile-title"
            });

            var _$info = $("<div></div>", {
                class: "tile-info"
            });

            var _$resetBtn = $("<div></div>", {
                class: "reset-time-btn control-btn"
            });

            var _$editBtn = $("<div></div>", {
                class: "edit-time-btn control-btn",
            });

            var _$panel = $("<div></div>", {
                class: "time-selection-panel-controls"
            });

            Object.defineProperties(self, {
                "$title": {
                    get: function () {
                        return _$title;
                    },
                    set: function (title) {
                        _$title.text(title);
                    }
                },
                "$info": {
                    get: function () {
                        return _$info;
                    },
                    set: function (info) {
                        _$info.text(info);
                    }
                },
                "$resetButton": {
                    get: function () {
                        return _$resetBtn;
                    }
                },
                "$editButton": {
                    get: function () {
                        return _$editBtn;
                    }
                }
            });
           
            self.$tile.addClass("time-toggle-tile")
                .prepend(self.$info)
                .prepend(self.$title);

            _$panel.append(_$editBtn);
            _$panel.append(_$resetBtn);
                
            self.$tile.append(_$panel);
            self.$tile.append($("<div></div>", {
                class: "separator-horizontal"
            }));
        };

        Controls.TimeToggleBtnTile.prototype = new Controls.Tile();

        Controls.DayBtnTile = function (source, resetBtnText) {
            var self = this;

            self.base = Controls.TimeToggleBtnTile;
            self.base(source, resetBtnText);

            var _$defaultHoursLabel = self.$tile.find(".default-hours-label");

            Object.defineProperties(self, {
                "$defaultHoursLabel": {
                    get: function () {
                        return _$defaultHoursLabel;
                    },
                }
            });
        };

        Controls.DayBtnTile.prototype = new Controls.TimeToggleBtnTile();

        Controls.YearBtnTile = function (source) {
            var self = this;

            self.base = Controls.DayBtnTile;
            self.base(source);

            var _$defaultDaysLabel = self.$tile.find(".default-days-label");

            Object.defineProperties(self, {
                "$defaultDaysLabel": {
                    get: function () {
                        return _$defaultDaysLabel;
                    },
                }
            });
        };

        Controls.YearBtnTile.prototype = new Controls.DayBtnTile();

        Controls.TimeSectionControlsPanel = function (source) {
            var self = this;

            self.base = Controls.NamedPanel;
            self.base(source);

            self.show();

            var _$resetButton = self.$panel.find(".reset-time-btn");

            var _yearBtn = new Controls.YearBtnTile(self.$content.find(".time-selection-years-tile"));
            _yearBtn.$title = "Years";
            _yearBtn.$editButton.prop("title", FC.Settings.EDIT_YEARS_TITLE);
            _yearBtn.$resetButton.prop("title", FC.Settings.RESET_YEARS_TITLE);

            var _dayBtn = new Controls.DayBtnTile(self.$content.find(".time-selection-days-tile"));
            _dayBtn.$title = "Days";
            _dayBtn.$editButton.prop("title", FC.Settings.EDIT_DAYS_TITLE);
            _dayBtn.$resetButton.prop("title", FC.Settings.RESET_DAYS_TITLE);

            var _hoursBtn = new Controls.TimeToggleBtnTile();
            _hoursBtn.$title = "Hours";
            _hoursBtn.$editButton.prop("title", FC.Settings.EDIT_HOURS_TITLE);
            _hoursBtn.$resetButton.prop("title", FC.Settings.RESET_HOURS_TITLE);

            Object.defineProperties(self, {
                "yearBtn": {
                    get: function () {
                        return _yearBtn;
                    }
                },
                "dayBtn": {
                    get: function () {
                        return _dayBtn;
                    }
                },
                "hourBtn": {
                    get: function () {
                        return _hoursBtn;
                    }
                },
                "$resetButton": {
                    get: function () {
                        return _$resetButton;
                    }
                }
            });

            _yearBtn.appendTo(self.$content);
            _dayBtn.appendTo(self.$content);
            _hoursBtn.appendTo(self.$content);
        };
        Controls.TimeSectionControlsPanel.prototype = new Controls.NamedPanel();


    })(FC.Controls || (FC.Controls = {}));
})(window.FC = window.FC || {}, jQuery);