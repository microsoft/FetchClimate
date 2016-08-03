(function (FC, $) {
    (function (Controls) {
        "use strict";

        Controls.TimeSelectControl = function (source) {
            var self = this;

            var _state = {
                status: "",
                temporal: {}
            };

            var _$control = typeof source !== "undefined" ? source : $("<div></div>");

            Object.defineProperties(self, {
                "$control": {
                    get: function () {
                        return _$control;
                    }
                },
                "state": {
                    get: function () {
                        return _state;
                    },
                    set: function (state) {
                        _state = state;
                    }
                }
            });

            self.appendTo = function ($parent) {
                _$control.appendTo($parent);
            };

            self.show = function (animation) {
                _$control.show.apply(_$control, animation);
            };

            self.hide = function (animation) {
                _$control.hide.apply(_$control, animation);
            };
        };

        Controls.TimeSelectYearsControl = function (source) {
            var self = this;

            self.base = Controls.TimeSelectControl;
            self.base(source);

            var _yearsPerRow = 10,
                _yearsPerColumn = 10,
                _yearsPerPage = _yearsPerRow * _yearsPerColumn,
                _minYear = 150,
                _pagesCount = 30,
                _currentPage,

                _startYear, // selected start year
                _endYear, // selected end year
                _mode, // selected mode: individual | avg | chunk
                _chunk, // selected chunk size

                _isInputCorrect = false,

                _$controlsPanel = self.$control.find(".control-controls"),
                _$startYear = _$controlsPanel.find("#start-year"),
                _$endYear = _$controlsPanel.find("#end-year"),
                _$modeSelector = _$controlsPanel.find(".time-control-select"),
                _$chunkContainer = _$controlsPanel.find(".chunk-select-container"),
                _$chunkSize = _$chunkContainer.find("input"),

                _$yearControl = self.$control.find(".control-content"),
                _$nav = _$yearControl.find(".years-nav"),
                _$prevPageBtn = _$nav.find(".prev-btn"),
                _$nextPageBtn = _$nav.find(".next-btn"),
                _$yearTable = _$yearControl.find("table");


            _$startYear.on("keyup", function () {
                _startYear = parseInt($(this).val(), 10);

                _isInputCorrect = _updateSelectedYears();
                if (_isInputCorrect) self.updateState();
            });

            _$endYear.on("keyup", function () {
                _endYear = parseInt($(this).val(), 10);

                _isInputCorrect = _updateSelectedYears();
                if (_isInputCorrect) self.updateState();
            });

            _$prevPageBtn.on("click", function () {
                _currentPage = Math.max(0, --_currentPage);
                
                _updatePageNavBtns();
                _updateYearsTable();
            });

            _$nextPageBtn.on("click", function () {
                _currentPage = Math.min(_pagesCount - 1, ++_currentPage);

                _updatePageNavBtns();
                _updateYearsTable();
            });

            _$chunkSize.on("keyup", function () {
                _chunk = parseInt($(this).val(), 10);

                _isInputCorrect = _updateSelectedYears();
                if (_isInputCorrect) self.updateState();
            });

            function updateEditBoxesConfig()
            {
                // show or hide chunk size selection container
                if ("chunk" === _mode) {
                    _$chunkContainer.addClass("visible");
                }
                else {
                    _$chunkContainer.removeClass("visible");
                }
            }

            _$modeSelector.on("change", function () {
                _mode = $(this).find("option:selected").val();
                updateEditBoxesConfig();
                _isInputCorrect = _updateSelectedYears();
                if (_isInputCorrect) self.updateState();
            });

            /**
            * Updates year for every year cell in table.
            */
            var _updateYearsTable = function updateYearsTable () {
                // start year of current page
                var year = _minYear + _currentPage * _yearsPerPage;

                for (var i = 0; i < _yearsPerColumn; i++) {
                    var $row = _$yearTable.find("tr:eq(" + i + ")");

                    for (var j = 0; j < _yearsPerRow; j++) {
                        var $cell = $row.find("td:eq(" + j + ")");
                        
                        $cell.text(year++);
                    }
                }

                _updateSelectedYears();
            };

            /**
             * Updates selection of year cells in year table. Cell is selected in mode:
             * 
             *  - avg: if cell's year is within selected year range;
             *  - indivudual: if cell's year is within selected year range;
             *  - chunk: if cell's year is within selected year range,
             *              chunk is within selected year range.
             */
            var _updateSelectedYears = function updateSelectedYears () {
                if (isNaN(_startYear) || _startYear === "" || isNaN(_endYear) || _endYear === "" || _startYear > _endYear) {
                    return false;
                }

                if (_mode === "chunk" && (isNaN(_chunk) || _chunk < 1 || _chunk > _endYear - _startYear + 1 || _chunk < 1)) {
                    return false;
                }

                _$yearTable.find("td")
                    .removeClass("selected average chunk")
                    .each(function () {
                        var $cell = $(this);
                        var year = parseInt($cell.text(), 10);

                        switch (_mode) {
                            case "avg":
                                if (_startYear <= year && year < _endYear) {
                                    $cell.addClass("selected average");
                                } // don't paint right border for last tile
                                else if (year === _endYear) {
                                    $cell.addClass("selected");
                                }

                                break;

                            case "individual":
                                if (_startYear <= year && year <= _endYear) {
                                    $cell.addClass("selected");
                                }

                                break;

                            case "chunk":
                                // count total chunks in selected year region
                                var totalChunks = Math.floor((_endYear - _startYear + 1) / _chunk);

                                // skip last chunk if it exceeds selected year range
                                if (0 !== (_endYear - _startYear - _chunk * totalChunks + 1) % _chunk &&
                                    year >= _startYear + _chunk * totalChunks
                                    ) {
                                    return false;
                                }

                                if (_startYear <= year && year <= _endYear) {
                                    $cell.addClass("selected");

                                    // don't paint right border for last tile in each chunk
                                    if (0 !== (year - _startYear + 1) % _chunk) {
                                        $cell.addClass("chunk");
                                    }
                                }

                                break;
                        }
                    });

                return true;
            };

            /**
             * Returns year part of TemporalDomain for given selection of year range and mode.
             * Returns false if year part of TemporalDomain can't be build.
             */
            var _updateYearTemporal = function updateYearTemporal() {
                var temporal = {
                    years: [],
                    yearCellMode: false
                };

                // year range or chunk is wrong
                if (_startYear > _endYear ||
                    ("chunk" === _mode && (_chunk > _endYear - _startYear + 1 || _chunk < 1))) {
                    return false;
                }

                switch (_mode) {
                    case "individual":
                        for (var i = _startYear; i <= _endYear; i++) {
                            temporal.years.push(i);
                        }

                        temporal.yearCellMode = false;

                        break;

                    case "avg":
                        temporal.years.push(_startYear);
                        temporal.years.push(_endYear+1);

                        temporal.yearCellMode = true;

                        break;

                    case "chunk":
                        for (i = _startYear; i <= _endYear + 1; i += _chunk) {
                            temporal.years.push(i);
                        }
                        

                        temporal.yearCellMode = true;

                        break;
                }

                return temporal;
            };

            /**
             * Disables or enables prev and next page navigation buttons depending on current page number.
             */
            var _updatePageNavBtns = function updatePageNavBtns () {
                if (0 === _currentPage) {
                    _$prevPageBtn.addClass("disabled");
                }
                else {
                    _$prevPageBtn.removeClass("disabled");
                }

                if (_pagesCount - 1 === _currentPage) {
                    _$nextPageBtn.addClass("disabled");
                }
                else {
                    _$nextPageBtn.removeClass("disabled");
                }
            };

            /**
            * Updates state of selected year region.
            */
            self.updateState = function updateState() {
                self.state = {
                    status: "",
                    temporal: {
                        years: [],
                        yearCellMode: false
                    }
                };

                self.state.status = _$modeSelector.find("option:selected").text();

                if ("chunk" === _mode) {
                    self.state.status = self.state.status + " of " +
                        _chunk +
                        " years each";
                }

                self.state.status = self.state.status + " from year " +
                    _startYear +
                    " to " + _endYear;

                var temporal = _updateYearTemporal();

                if (temporal) {
                    self.state.temporal = temporal;
                    self.$control.trigger("yearsstatechanged");
                }
            };

            /**
             * Initializes control.
             */

            var _pivotYear;

            function onMouseDown(e) {
                var year = parseInt(e.target.innerText);
                if (!isNaN(year)) {
                    _pivotYear = year;
                    _$startYear.val(year);
                    _$endYear.val(year);
                    _startYear = _endYear = year;
                    _isInputCorrect = _updateSelectedYears();
                    if (_isInputCorrect) self.updateState();
                    FC.isLeftButtonDown = true;
                }
            }

            function onMouseOver(e) {
                if (!FC.isLeftButtonDown)
                    return;
                var year = parseInt(e.target.innerText);
                if (!isNaN(year)) {
                    _startYear = Math.min(year, _pivotYear);
                    _endYear = Math.max(year, _pivotYear);
                    _$startYear.val(_startYear);
                    _$endYear.val(_endYear);
                    _isInputCorrect = _updateSelectedYears();
                    if (_isInputCorrect) self.updateState();
                }
            }

            self.initialize = function initialize (temporal) {
                // add year cells to table
                _$yearTable.empty();
                for (var i = 0; i < _yearsPerColumn; i++) {
                    var _$row = $("<tr></tr>").appendTo(_$yearTable);

                    for (var j = 0; j < _yearsPerRow; j++) {
                        $("<td></td>", {
                            class: "year-table-cell"
                        }).appendTo(_$row).
                            on("mousedown", onMouseDown).
                            on("mouseover", onMouseOver);
                    }
                }

                var yearsIntervalsCount = temporal.years.length - (temporal.yearCellMode ? 1 : 0);
                var yearsInInterval = temporal.years.length > 1 ? temporal.years[1] - temporal.years[0] : 1;

                _startYear = temporal.years[0];
                _endYear = temporal.years[temporal.years.length - 1];
                if (temporal.yearCellMode)
                    _endYear -= 1;
                _currentPage = Math.max(0,
                    Math.min(_pagesCount - 1,
                        Math.floor((_startYear - _minYear) / _yearsPerPage)));

                _updatePageNavBtns();
                _chunk = 3;
                _mode = "individual";

                if (temporal.yearCellMode) {
                    if (1 === yearsIntervalsCount) {
                        _mode = "avg";
                    }
                    else {
                        _mode = "chunk";
                        _chunk = yearsInInterval;                        
                    }
                }
                else {
                    _mode = "individual";
                }

                _$startYear.val(_startYear);
                _$endYear.val(_endYear);
                _$chunkSize.val(_chunk);

                _$modeSelector.find("option[value='" + _mode + "']").prop("selected", true);
                updateEditBoxesConfig();

                _updateYearsTable();
                self.updateState();
            };
        };
        Controls.TimeSelectYearsControl.prototype = new Controls.TimeSelectControl();

        Controls.TimeSelectDaysControl = function (source) {
            var self = this;

            self.base = Controls.TimeSelectControl;
            self.base(source);

            var _months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"],
                _daysInMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31],

                _startDay, // selected start day
                _endDay, // selected end day
                _mode, // active mode: wholeday | avg | individual | chunk
                _chunk, // chunk size
                _isLeap, // indicates whethe day calendar should include 29 of Ferbuary or not

                _isInputCorrect = false,

                _$controlsPanel = self.$control.find(".control-controls"),
                _$startDay = _$controlsPanel.find("#start-day"),
                _$endDay = _$controlsPanel.find("#end-day"),
                _$modeSelector = _$controlsPanel.find(".time-control-select"),
                _$chunkContainer = _$controlsPanel.find(".chunk-select-container"),
                _$daysSelectCointainer = _$controlsPanel.find(".days-select-container"),
                _$chunkSize = _$chunkContainer.find("input"),

                _$dayControl = self.$control.find(".control-content"),
                _$dayTable = _$dayControl.find("table");

            _$startDay.on("keyup", function () {
                _startDay = parseInt($(this).val(), 10);

                _isInputCorrect =_updateSelectedDays();
                if(_isInputCorrect) self.updateState();
            });

            _$endDay.on("keyup", function () {
                _endDay = parseInt($(this).val(), 10);

                _isInputCorrect = _updateSelectedDays();
                if (_isInputCorrect) self.updateState();
            });

            _$chunkSize.on("keyup", function () {
                _chunk = parseInt($(this).val(), 10);

                _isInputCorrect = _updateSelectedDays();
                if (_isInputCorrect) self.updateState();
            });

            function updateEditBoxesConfig()
            {
                // show or hide chunk size selection container
                if ("chunk" === _mode) {
                    _$chunkContainer.addClass("visible");
                }
                else {
                    _$chunkContainer.removeClass("visible");
                }

                // show or hide days selection container
                if ("wholeyear" === _mode || "monthlyavg" === _mode) {
                    _$daysSelectCointainer.removeClass("visible");
                }
                else {
                    _$daysSelectCointainer.addClass("visible");
                }
            }

            _$modeSelector.on("change", function () {
                _mode = $(this).find("option:selected").val();
                updateEditBoxesConfig();
                _isInputCorrect = _updateSelectedDays();
                if (_isInputCorrect) self.updateState();
            });

            /**
             * Updates selection of day cells in day table. Cell is selected in mode:
             *
             *  - wholeyear: every cell is selected
             *  - monthlyavg: every cell is selected
             *  - avg: if cell's day is within selected day range;
             *  - indivudual: if cell's day is within selected day range;
             *  - chunk: if cell's day is within selected day range,
             *              chunk is within selected day range.
             */
            var _updateSelectedDays = function updateSelectedDays () {
                if ((_mode === "avg" || _mode === "individual" || _mode === "chunk") &&
                    (isNaN(_startDay) || _startDay === "" || isNaN(_endDay) || _endDay === "" || _startDay > _endDay)) {
                    return false;
                }

                if (_mode === "chunk" && (isNaN(_chunk) || _chunk < 1 || _chunk > _endDay - _startDay + 1 || _chunk < 1)) {
                    return false;
                }

                var $rows = _$dayTable.find("tr.days-table-row");
            
                $rows.find("td").removeClass("selected average chunk");

                $rows.find("td").each(function () {
                    var $cell = $(this);
                    var day = parseInt($cell.text(), 10);

                    if (isNaN(_startDay) || isNaN(_endDay)) {
                        return false;
                    }

                    switch (_mode) {
                        case "wholeyear":
                            $cell.addClass("selected average");
                            break;

                        case "monthlyavg":
                            $cell.addClass("selected average");
                            break;

                        case "avg":
                            if (_startDay <= day && day < _endDay) {
                                $cell.addClass("selected average");
                            } // don't paint right border of last tile
                            else if (day === _endDay) {
                                $cell.addClass("selected");
                            }
                            break;

                        case "individual":
                            if (_startDay <= day && day <= _endDay) {
                                $cell.addClass("selected");
                            }
                            break;

                        case "chunk":
                            // count total chunks in selected year region
                            var totalChunks = Math.floor((_endDay - _startDay + 1) / _chunk);

                            // skip last chunk if some of its tiles exceed years region
                            if (0 !== (_endDay - _startDay - _chunk * totalChunks + 1) % _chunk &&
                                day >= _startDay + _chunk * totalChunks
                                ) {
                                    return false;
                            }

                            if (_startDay <= day && day <= _endDay) {
                                $cell.addClass("selected");

                                // don't paint right border of last tile in each chunk
                                if (0 !== (day - _startDay + 1) % _chunk) {
                                    $cell.addClass("chunk");
                                }
                            }
                            break;
                    }
                });

                return true;
            };

            /**
             * Returns day part of TemporalDomain for given selection of day range and mode.
             * Returns false if day part of TemporalDomain can't be build.
             */
            var _updateDayTemporal = function updateDayTemporal () {
                var temporal = {
                    days: [],
                    dayCellMode: false
                };

                // day range or chunk is wrong
                if ((_mode === "avg" || _mode === "individual" || _mode === "chunk") && (_startDay > _endDay ||
                    ("chunk" === _mode && (_chunk > _endDay - _startDay + 1 || _chunk < 1)))) {
                    return false;
                }

                switch (_mode) {
                    case "wholeyear":
                        temporal.days.push(1);
                        temporal.days.push(366 + (_isLeap ? 1 : 0));

                        temporal.dayCellMode = true;

                        break;

                    case "monthlyavg":
                        var day = 1;
                        temporal.days.push(day);

                        for (var i = 0; i < _months.length; i++) {
                            day += _daysInMonth[i];
                            temporal.days.push(day);
                        }

                        temporal.dayCellMode = true;

                        break;

                    case "individual":
                        for (i = _startDay; i <= _endDay; i++) {
                            temporal.days.push(i);
                        }

                        temporal.dayCellMode = false;

                        break;

                    case "avg":
                        temporal.days.push(_startDay);
                        temporal.days.push(_endDay+1);

                        temporal.dayCellMode = true;

                        break;

                    case "chunk":
                        for (i = _startDay; i <= _endDay + 1; i += _chunk) {
                            temporal.days.push(i);
                        }
                       

                        temporal.dayCellMode = true;

                        break;
                }

                return temporal;
            };

            /**
             * Redraws day cells in days table.
             *
             * @param isLeap (string)  indicates whether 29 of February is included or not
             */
            self.updateDaysTable = function updateDaysTable (isLeap) {
                var _currentDay = 1;
                _isLeap = isLeap;

                // clear days cells
                _$dayTable.find("tr.days-table-row").remove();
                
                for (var i = 0; i < _months.length; i++) {
                    var _$row = $("<tr></tr>", {
                        class: "days-table-row",
                        "data-month": _months[i]
                    }).appendTo(_$dayTable);

                    var j = 0;

                    // add 29 days to February
                    if (1 === i && _isLeap) {
                        for (; j < _daysInMonth[i] + 1; j++) {
                            $("<td></td>", {
                                class: "day-table-cell",
                                text: _currentDay++
                            }).appendTo(_$row).on("mousedown", onMouseDown).on("mouseover", onMouseOver);
                        }
                    }
                    else {
                        for (; j < _daysInMonth[i]; j++) {
                            $("<td></td>", {
                                class: "day-table-cell",
                                text: _currentDay++
                            }).appendTo(_$row).on("mousedown", onMouseDown).on("mouseover", onMouseOver);
                        }
                    }
                }

                _updateSelectedDays();
            };

            /**
            * Updates state of selected day region.
            */
            self.updateState = function updateState () {
                self.state = {
                    status: "",
                    temporal: {
                        days: [],
                        dayCellMode: false
                    }
                };

                self.state.status = _$modeSelector.find("option:selected").text();

                if ("chunk" === _mode) {
                    self.state.status = self.state.status + " of " +
                        _chunk +
                        " days each";
                }

                if ("individual" === _mode || "chunk" === _mode || "avg" === _mode) {
                    self.state.status = self.state.status + " from day " +
                        _startDay +
                        " to " + _endDay;
                }

                var temporal = self.state.temporal = _updateDayTemporal();

                if (temporal) {
                    self.state.temporal = temporal;
                    self.$control.trigger("daysstatechanged");
                }
            };

            var _pivotDay;

            function onMouseDown(e) {
                var day = parseInt(e.target.innerText);
                if (!isNaN(day)) {
                    _pivotDay = day;
                    _$startDay.val(day);
                    _$endDay.val(day);
                    _startDay = _endDay = day;
                    _isInputCorrect = _updateSelectedDays();
                    if (_isInputCorrect) self.updateState();
                    FC.isLeftButtonDown = true;
                }
            }

            function onMouseOver(e) {
                if (!FC.isLeftButtonDown)
                    return;
                var day = parseInt(e.target.innerText);
                if (!isNaN(day)) {
                    _startDay = Math.min(day, _pivotDay);
                    _endDay = Math.max(day, _pivotDay);
                    _$startDay.val(_startDay);
                    _$endDay.val(_endDay);
                    _isInputCorrect = _updateSelectedDays();
                    if (_isInputCorrect) self.updateState();
                }
            }

            /**
             * Initializes control
             */
            self.initialize = function initialize (temporal) {
                // add "zero" row (days number row)
                _$dayTable.empty();
                var _$row = $("<tr></tr>").appendTo(_$dayTable);
                for (var i = 1; i <= 31; i++) {
                    $("<td></td>", {
                        class: "day-table-cell",
                        text: i
                    }).appendTo(_$row);
                }

                var daysIntervalsCount = temporal.days.length - (temporal.dayCellMode ? 1 : 0);
                var isDaysIntervals = temporal.dayCellMode;
                var daysInInterval = temporal.days.length > 1 ? temporal.days[1] - temporal.days[0] : 1;
                var isMonthlySelection = FC.isMonthlyCellAxis(FC.state.temporal.days);
                
                _startDay = temporal.days[0];
                _endDay = temporal.days[temporal.days.length - 1];
                if (temporal.dayCellMode)
                    _endDay -= 1;
                _chunk = 30;

                // check for leap "year" (end year equals to start year and it's leap)
                _isLeap = false;
                if (1 === FC.state.temporal.years.length || (2 === FC.state.temporal.years.length &&
                    FC.state.temporal.years[0] === FC.state.temporal.years[1])) {
                    _isLeap = FC.isLeapYear(FC.state.temporal.years[0]);
                }

                if (isMonthlySelection) {
                    _mode = "monthlyavg";
                }
                else if (isDaysIntervals) {
                    if (1 === daysIntervalsCount) {
                        if (1 === _startDay && 366 + (_isLeap ? 1 : 0) === _endDay) {
                            _mode = "wholeyear";
                        }
                        else {
                            _mode = "avg";
                        }
                    }
                    else {
                        _mode = "chunk";
                        _chunk = daysInInterval;                        
                    }
                }
                else {
                    _mode = "individual";
                }

                _$modeSelector.find("option[value='" + _mode + "']").prop("selected", true);
                updateEditBoxesConfig();

                _$startDay.val(_startDay);
                _$endDay.val(_endDay);
                _$chunkSize.val(_chunk);

                self.updateDaysTable();
                self.updateState();
            };
        };
        Controls.TimeSelectDaysControl.prototype = new Controls.TimeSelectControl();

        Controls.TimeSelectHoursControl = function (source) {
            var self = this;

            self.base = Controls.TimeSelectControl;
            self.base(source);

            var _hoursCount = 24,

                _startHour, // selected start hour
                _endHour, // selected end hour
                _mode, // active mode: wholeday | avg | individual | chunk
                _chunk, // chunk size

                _isInputCorrect = false,

                _$controlsPanel = self.$control.find(".control-controls"),
                _$startHour = _$controlsPanel.find("#start-hour"),
                _$endHour = _$controlsPanel.find("#end-hour"),
                _$modeSelector = _$controlsPanel.find(".time-control-select"),
                _$chunkContainer = _$controlsPanel.find(".chunk-select-container"),
                _$hoursSelectCointainer = _$controlsPanel.find(".hours-select-container"),
                _$chunkSize = _$chunkContainer.find("input"),

                _$hourControl = self.$control.find(".control-content"),
                _$hourTable = _$hourControl.find("table");

            _$startHour.on("keyup", function () {
                _startHour = parseInt($(this).val(), 10);

                _isInputCorrect = _updateSelectedHours();
                if (_isInputCorrect) self.updateState();
            });

            _$endHour.on("keyup", function () {
                _endHour = parseInt($(this).val(), 10);

                _isInputCorrect = _updateSelectedHours();
                if (_isInputCorrect) self.updateState();
            });

            _$chunkSize.on("keyup", function () {
                _chunk = parseInt($(this).val(), 10);

                _isInputCorrect = _updateSelectedHours();
                if (_isInputCorrect) self.updateState();
            });

            function updateEditBoxesConfig()
            {
                // show or hide chunk size selection container
                if ("chunk" === _mode) {
                    _$chunkContainer.addClass("visible");
                }
                else {
                    _$chunkContainer.removeClass("visible");
                }

                // show or hide hour range selection
                if ("wholeday" === _mode) {
                    _$hoursSelectCointainer.removeClass("visible");
                }
                else {
                    _$hoursSelectCointainer.addClass("visible");
                }
            }

            _$modeSelector.on("change", function () {
                _mode = $(this).find("option:selected").val();
                updateEditBoxesConfig();
                _isInputCorrect = _updateSelectedHours();
                if (_isInputCorrect) self.updateState();
            });

            /**
             * Updates selection of hour cells in hour table. Cell is selected in mode:
             *
             *  - wholeday: every cell is selected
             *  - avg: if cell's hour is within selected hour range;
             *  - indivudual: if cell's hour is within selected hour range;
             *  - chunk: if cell's hour is within selected hour range,
             *              chunk is within selected hour range.
             */
            var _updateSelectedHours = function updateSelectedHours () {
                if ((_mode === "avg" || _mode === "individual" || _mode === "chunk") &&
                    (isNaN(_startHour) || _startHour === "" || isNaN(_endHour) || _endHour === "" || _startHour > _endHour)) {
                    return false;
                }

                if (_mode === "chunk" && (isNaN(_chunk) || _chunk < 1 || _chunk > _endHour - _startHour + 1 || _chunk < 1)) {
                    return false;
                }

                _$hourTable.find("td")
                    .removeClass("selected average chunk")
                    .each(function (index) {
                        var $cell = $(this);
                        var hour = index;

                        switch (_mode) {
                            case "wholeday":
                                $cell.addClass("selected average");
                                break;

                            case "avg":
                                if (_startHour <= hour && hour < _endHour) {
                                    $cell.addClass("selected average");
                                } // don't paint right border for last tile
                                else if (hour === _endHour) {
                                    $cell.addClass("selected");
                                }
                                break;

                            case "individual":
                                if (_startHour <= hour && hour <= _endHour) {
                                    $cell.addClass("selected");
                                }
                                break;

                            case "chunk":
                                // count total chunks in selected year region
                                var totalChunks = Math.floor((_endHour - _startHour + 1) / _chunk);

                                // skip last chunk if some of its tiles exceed years region
                                if (0 !== (_endHour - _startHour - _chunk * totalChunks + 1) % _chunk &&
                                    hour >= _startHour + _chunk * totalChunks
                                    ) {
                                        return false;
                                }

                                if (_startHour <= hour && hour <= _endHour) {
                                    $cell.addClass("selected");

                                    // don't paint right border for last tile in each chunk
                                    if (0 !== (hour - _startHour + 1) % _chunk) {
                                        $cell.addClass("chunk");
                                    }
                                }
                                break;
                        }
                    });

                return true;
            };

            /**
             * Returns hour part of TemporalDomain for given selection of hour range and mode.
             * Returns false if hour part of TemporalDomain can't be build.
             */
            var _updateHourTemporal = function updateHourTemporal () {
                var temporal = {
                    hours: [],
                    hourCellMode: false
                };

                // hour range or chunk is wrong
                if ((_mode === "avg" || _mode === "individual" || _mode === "chunk")  && (_startHour > _endHour ||
                    ("chunk" === _mode && (_chunk > _endHour - _startHour + 1 || _chunk < 1)))) {
                    return false;
                }

                switch (_mode) {
                    case "wholeday":
                        temporal.hours.push(0);
                        temporal.hours.push(24);

                        temporal.hourCellMode = true;

                        break;

                    case "individual":
                        for (var i = Math.max(0, _startHour);
                            i <= Math.min(_hoursCount - 1, _endHour);
                            i++) {
                            temporal.hours.push(i);
                        }

                        temporal.hourCellMode = false;

                        break;

                    case "avg":
                        temporal.hours.push(Math.max(0, _startHour));
                        temporal.hours.push(Math.min(_hoursCount, _endHour + 1));

                        temporal.hourCellMode = true;

                        break;

                    case "chunk":
                        for (i = Math.max(0, _startHour);
                            i <= Math.min(_hoursCount, _endHour + 1);
                            i += _chunk) {
                            temporal.hours.push(i);
                        }

                        temporal.hourCellMode = true;

                        break;
                }

                return temporal;
            };

            /**
            * Updates status string (state) of selected hour region.
            */
            self.updateState = function updateState () {
                self.state = {
                    status: "",
                    temporal: {
                        hours: [],
                        hourCellMode: false
                    }
                };

                self.state.status = _$modeSelector.find("option:selected").text();

                if ("chunk" === _mode) {
                    self.state.status = self.state.status + " of " +
                        _chunk +
                        " hours each";
                }

                if ("individual" === _mode || "chunk" === _mode || "avg" === _mode) {
                    self.state.status = self.state.status + " from hour " +
                        Math.max(0, _startHour) +
                        " to " + Math.min(_hoursCount - 1, _endHour);
                }

                var temporal = self.state.temporal = _updateHourTemporal();

                if (temporal) {
                    self.state.temporal = temporal;
                    self.$control.trigger("hoursstatechanged");
                }
            };

            var _pivotHour;

            function onMouseDown(e) {
                var hour = parseInt(e.target.innerText);
                if (!isNaN(hour)) {
                    _pivotHour = hour;
                    _$startHour.val(hour);
                    _$endHour.val(hour);
                    _startHour = _endHour = hour;
                    _isInputCorrect = _updateSelectedHours();
                    if (_isInputCorrect) self.updateState();
                    FC.isLeftButtonDown = true;
                }
            }

            function onMouseOver(e) {
                if (!FC.isLeftButtonDown)
                    return;
                var hour = parseInt(e.target.innerText);
                if (!isNaN(hour)) {
                    _startHour = Math.min(hour, _pivotHour);
                    _endHour = Math.max(hour, _pivotHour);
                    _$startHour.val(_startHour);
                    _$endHour.val(_endHour);
                    _isInputCorrect = _updateSelectedHours();
                    if (_isInputCorrect) self.updateState();
                }
            }

            /**
             * Initializes control.
             */
            self.initialize = function initialize (temporal) {
                // create hour cells
                _$hourTable.empty();
                var _$row = $("<tr></tr>").appendTo(_$hourTable);
                for (var i = 0; i < _hoursCount; i++) {
                    $("<td></td>", {
                        class: "hour-table-cell",
                        text: i
                    }).appendTo(_$row).on("mousedown", onMouseDown).on("mouseover", onMouseOver);
                }

                var hoursIntervalsCount = temporal.hours.length - (temporal.hourCellMode ? 1 : 0);
                var hourCellMode = temporal.hourCellMode;
                var hoursInInterval = temporal.hours.length > 1 ? temporal.hours[1] - temporal.hours[0] : 1;

                _startHour = temporal.hours[0];
                _endHour = temporal.hours[temporal.hours.length - 1];
                _chunk = 6;

                if (hourCellMode) {
                    if (1 === hoursIntervalsCount) {
                        if (_hoursCount === hoursInInterval) {
                            _mode = "wholeday";
                        }
                        else {
                            _mode = "avg";

                            // In 'avg' mode end hour in URL is greater by one than actual end year.
                            --_endHour;
                        }
                    }
                    else {
                        _mode = "chunk";
                        _chunk = hoursInInterval;

                        // in chunk mode end hour in Temporal Domain is greater by one than actual end hour
                        --_endHour;
                    }
                }
                else {
                    _mode = "individual";
                }

                if (_endHour >= _hoursCount) _endHour = _hoursCount - 1;

                _$modeSelector.find("option[value='" + _mode + "']").prop("selected", true);
                updateEditBoxesConfig();
                
                _$startHour.val(_startHour);
                _$endHour.val(_endHour);
                _$chunkSize.val(_chunk);

                _updateSelectedHours();
                self.updateState();
            };
        };
        Controls.TimeSelectHoursControl.prototype = new Controls.TimeSelectControl();

    })(FC.Controls || (FC.Controls = {}));
})(window.FC = window.FC || {}, jQuery);