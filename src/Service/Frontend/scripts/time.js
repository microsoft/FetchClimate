(function (FC, $) {
    (function (Time) {
        "use strict";

        var _$page,

            _timeSelectorPanel,
            _selectedTimeMode,
            _activeTimeControl,

            _yearControl,
            _dayControl,
            _hourControl,

            _$yearsTabHeader, _$daysTabHeader, _$hoursTabHeader, _$selectedTabHeader;

        function getDefaultDomain() {
            var temporalDomain = new FC.TemporalDomainBuilder();
            temporalDomain.parseYearCells(FC.state.config.boundaries.yearMin + "," + FC.state.config.boundaries.yearMax);
            temporalDomain.parseDayCells("1,366");
            temporalDomain.parseHourCells("0,24");
            return temporalDomain;
        }

        function isDefaultYearsDomain(d) {
            return d.yearCellMode && d.years.length == 2 && d.years[0] == FC.state.config.boundaries.yearMin && d.years[1] == FC.state.config.boundaries.yearMax;
        }

        function isDefaultDaysDomain(d) {
            return d.dayCellMode && d.days.length == 2 && d.days[0] == 1 && d.days[1] == 366;
        }

        function isDefaultHoursDomain(d) {
            return d.hourCellMode && d.hours.length == 2 && d.hours[0] == 0 && d.hours[1] == 24;
        }

        function isDefaultDomain(d) {
            return isDefaultYearsDomain(d) && isDefaultDaysDomain(d) && isDefaultHoursDomain(d);
        }

        Time.initialize = function () {
            _$page = $("section.time");

            if (!FC.state.temporal) {
                var temporalDomain = getDefaultDomain();
                FC.state.setTemporal(temporalDomain.getTemporalDomain());
                if (FC.Results) FC.Results.updateSliders();
            }

            _timeSelectorPanel = _$page.find(".time-selection-panel");
            _timeSelectorPanel = new FC.Controls.TimeSectionControlsPanel(_timeSelectorPanel);

            _yearControl = _$page.find(".time-selection-control.year-control");
            _yearControl = new FC.Controls.TimeSelectYearsControl(_yearControl);

            _dayControl = _$page.find(".time-selection-control.day-control");
            _dayControl = new FC.Controls.TimeSelectDaysControl(_dayControl);

            _hourControl = _$page.find(".time-selection-control.hour-control");
            _hourControl = new FC.Controls.TimeSelectHoursControl(_hourControl);

            // Time control state changed event handlers.
            // Update tile control info text and Temporal Domain.

            function updateUI() {
                if(isDefaultDomain(FC.state.temporal))
                    _timeSelectorPanel.$resetButton.addClass("hidden");
                else
                    _timeSelectorPanel.$resetButton.removeClass("hidden");

                if (isDefaultYearsDomain(FC.state.temporal)) {
                    _timeSelectorPanel.yearBtn.$resetButton.addClass("hidden");
                } else
                    _timeSelectorPanel.yearBtn.$resetButton.removeClass("hidden");

                var isDefaultDays = isDefaultDaysDomain(FC.state.temporal);
                var isDefaultHours = isDefaultHoursDomain(FC.state.temporal);

                if (isDefaultDays) {
                    _timeSelectorPanel.yearBtn.$defaultDaysLabel.removeClass("hidden");
                    _timeSelectorPanel.dayBtn.$tile.addClass("hidden");
                } else {
                    _timeSelectorPanel.yearBtn.$defaultDaysLabel.addClass("hidden");
                    _timeSelectorPanel.dayBtn.$tile.removeClass("hidden");
                }

                if (isDefaultHours) {
                    _timeSelectorPanel.dayBtn.$defaultHoursLabel.removeClass("hidden");
                    _timeSelectorPanel.hourBtn.$tile.addClass("hidden");
                } else {
                    _timeSelectorPanel.dayBtn.$defaultHoursLabel.addClass("hidden");
                    _timeSelectorPanel.hourBtn.$tile.removeClass("hidden");
                }

                if (isDefaultHours && isDefaultDays) 
                    _timeSelectorPanel.yearBtn.$defaultHoursLabel.removeClass("hidden");
                else
                    _timeSelectorPanel.yearBtn.$defaultHoursLabel.addClass("hidden");
            }

            _yearControl.$control.on("yearsstatechanged", function () {
                _timeSelectorPanel.yearBtn.$info = _yearControl.state.status;

                var domain = new FC.TemporalDomain(
                    _yearControl.state.temporal.years, _yearControl.state.temporal.yearCellMode,
                    FC.state.temporal.days, FC.state.temporal.dayCellMode,
                    FC.state.temporal.hours, FC.state.temporal.hourCellMode);
                FC.state.setTemporal(domain);
                updateUI();
            });

            _dayControl.$control.on("daysstatechanged", function () {
                _timeSelectorPanel.dayBtn.$info = _dayControl.state.status;

                var domain = new FC.TemporalDomain(
                    FC.state.temporal.years, FC.state.temporal.yearCellMode,
                    _dayControl.state.temporal.days, _dayControl.state.temporal.dayCellMode,
                    FC.state.temporal.hours, FC.state.temporal.hourCellMode);
                FC.state.setTemporal(domain);
                updateUI();
            });

            _hourControl.$control.on("hoursstatechanged", function () {
                _timeSelectorPanel.hourBtn.$info = _hourControl.state.status;

                var domain = new FC.TemporalDomain(
                    FC.state.temporal.years, FC.state.temporal.yearCellMode,
                    FC.state.temporal.days, FC.state.temporal.dayCellMode,
                    _hourControl.state.temporal.hours, _hourControl.state.temporal.hourCellMode);
                FC.state.setTemporal(domain);
                updateUI();
            });

            // Time control click event handlers.

            _timeSelectorPanel.$resetButton.on("click", function () {
                var temporalDomain = getDefaultDomain().getTemporalDomain();
                FC.state.setTemporal(temporalDomain);
                _yearControl.initialize(temporalDomain);
                _dayControl.initialize(temporalDomain);
                _hourControl.initialize(temporalDomain);
            });

            _timeSelectorPanel.yearBtn.$resetButton.on("click", function () {
                var temporalDomain = FC.state.temporal;
                temporalDomain.yearCellMode = true;
                temporalDomain.years = [FC.state.config.boundaries.yearMin, FC.state.config.boundaries.yearMax];
                FC.state.setTemporal(temporalDomain);
                _yearControl.initialize(temporalDomain);
            });

            _timeSelectorPanel.dayBtn.$resetButton.on("click", function () {
                var temporalDomain = FC.state.temporal;
                temporalDomain.dayCellMode = true;
                temporalDomain.days = [1, 366];
                FC.state.setTemporal(temporalDomain);
                _dayControl.initialize(temporalDomain);
            });

            _timeSelectorPanel.hourBtn.$resetButton.on("click", function () {
                var temporalDomain = FC.state.temporal;
                temporalDomain.hourCellMode = true;
                temporalDomain.hours = [0, 24];
                FC.state.setTemporal(temporalDomain);
                _hourControl.initialize(temporalDomain);
            });

            _$selectedTabHeader = _$yearsTabHeader = _$page.find('.time-selection-tab-header[data-mode="years"]');
            _$daysTabHeader = _$page.find('.time-selection-tab-header[data-mode="days"]');
            _$hoursTabHeader = _$page.find(".time-selection-tab-header[data-mode='hours']");

            function setYearsTimeMode() {
                if (_selectedTimeMode === _timeSelectorPanel.yearBtn.$tile) 
                    return;

                if (_selectedTimeMode) {
                    _selectedTimeMode.removeClass("selected");
                    _activeTimeControl.hide();
                }
                _$selectedTabHeader.removeClass("selected");

                (_$selectedTabHeader = _$yearsTabHeader).addClass("selected");
                (_selectedTimeMode = _timeSelectorPanel.yearBtn.$tile).addClass("selected");
                
                _yearControl.show();
                _activeTimeControl = _yearControl;
            }
            _$yearsTabHeader.on("click", setYearsTimeMode);
            _timeSelectorPanel.yearBtn.$editButton.on("click", setYearsTimeMode);

            function setDaysTimeMode() {
                if (_selectedTimeMode === _timeSelectorPanel.dayBtn.$tile)
                    return;

                if (_selectedTimeMode) {
                    _selectedTimeMode.removeClass("selected");
                    _activeTimeControl.hide();
                }
                _$selectedTabHeader.removeClass("selected");

                (_selectedTimeMode = _timeSelectorPanel.dayBtn.$tile).addClass("selected");
                (_$selectedTabHeader = _$daysTabHeader).addClass("selected");

                var isLeap = false;
                if (1 === FC.state.temporal.years.length || (2 === FC.state.temporal.years.length &&
                    FC.state.temporal.years[0] === FC.state.temporal.years[1])) {
                    isLeap = FC.isLeapYear(FC.state.temporal.years[0]);
                }
                _dayControl.updateDaysTable(isLeap);
                _dayControl.show();
                _activeTimeControl = _dayControl;
            }

            _$daysTabHeader.on("click", setDaysTimeMode);
            _timeSelectorPanel.dayBtn.$editButton.on("click", setDaysTimeMode);

            function setHoursTimeMode() {
                if (_selectedTimeMode === _timeSelectorPanel.hourBtn.$tile) 
                    return;

                if (_selectedTimeMode) {
                    _selectedTimeMode.removeClass("selected");
                    _activeTimeControl.hide();
                }
                _$selectedTabHeader.removeClass("selected");

                (_selectedTimeMode = _timeSelectorPanel.hourBtn.$tile).addClass("selected");
                (_$selectedTabHeader = _$hoursTabHeader).addClass("selected");

                _hourControl.show();
                _activeTimeControl = _hourControl;
            }

            _$hoursTabHeader.on("click", setHoursTimeMode);
            _timeSelectorPanel.hourBtn.$editButton.on("click", setHoursTimeMode);

            _yearControl.initialize(FC.state.temporal);
            _dayControl.initialize(FC.state.temporal);
            _hourControl.initialize(FC.state.temporal);

            // set year time control as active
            _timeSelectorPanel.yearBtn.$editButton.trigger("click");
        };

    })(FC.Time || (FC.Time = {}));
})(window.FC = window.FC || {}, jQuery);