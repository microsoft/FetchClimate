(function ($) {
    "use strict";

    /**
    * Fuzzy searches query in .html() (original source string should be stored in data("name"))
    * of given jQuery element. Given element shouldn't contain any nested html elements.
    * Highlightes matched characters if text matches fuzzy search,with span element, style this
    * element in your stylesheet. If text doesn't match fuzzy search of query string, then no
    * character is highlighted. 
    */
    $.fn.fuzzyHighlight = function (query) {        
        // reset formatting of element
        var text = $(this).data("name");
        $(this).html(text);
        text = text.toLowerCase();

        if (!text.fuzzy(query)) {
            return false;
        }

        var i = 0,
            index = 0,
            character;
        query = query.toLowerCase();
        for (; character = query[i++];) {
            // to highlight element span element is added in html, because of that
            // length of text and html differs. Characters should be searched in html
            // of given element.
            if ((index = $(this).html().toLowerCase().indexOf(character, index)) === -1) {
                return false;
            }
            else {
                // html contains case sensitive characters
                $(this).html($(this).html().replaceAt(index, "<span>" + $(this).html().charAt(index) + "</span>"));
                index += 13; // 13 is count of characters in "<span></span>"
            }
        }
    };

    $.fn.highlightSubstring = function (query) {
        // reset formatting of element
        var text = $(this).data("name");
        var pattern = new RegExp(query, "gi");
        $(this).html(text);

        if (pattern.test(text)) {
            $(this).html($(this).html().replace(pattern, "<span>$&</span>"));
        }
    };

    $.fn.labeledSlider = function (options) {
        var self = this;
        var _$labelTemplate = $("<span></span>").addClass("fc-slider-value-label");
        var smoothness = FC.Settings.SLIDER_SMOOTHNESS;
        var lastKeyEventTime = 0;

        function onSliderCreate(event, ui) {
            // jshint validthis: true
            var $this = $(this);
            $this.find(".ui-slider-handle").append(_$labelTemplate.clone());
        }

        function onSliderStart(event, ui) {
            // jshint validthis: true
            var $this = $(this);
            var $label = $this.find(".fc-slider-value-label").show();
            var index = Math.round(ui.value / smoothness);
            var value = $this.data("labels")[index];
            updateValueLabel($label, value);
            $this.data("index", index);
            $this.data("value", value);
            $this.trigger("change", [index]);
        }

        function onSliderSlide(event, ui) {
            // jshint validthis: true
            var $this = $(this);
            var $label = $this.find(".fc-slider-value-label");
            var prevIndex = $this.data("index");
            var index = Math.round(ui.value / smoothness);
            var length = $this.data("labels") instanceof Array ? $this.data("labels").length : 0;
            var value;
            var keyEventTime = +(new Date());
            var keyEventInterval = keyEventTime - lastKeyEventTime; //how many milliseconds since the last request

            // Handle arrow keys with throttle.
            if (event.keyCode >= 37 && event.keyCode <= 40 && keyEventInterval > 60) {
                lastKeyEventTime = keyEventTime;

                if (event.keyCode == 37 || event.keyCode == 40) {
                    index = (index - 1 < 0) ? 0 : (index - 1);
                } else if (event.keyCode == 39 || event.keyCode == 38) {
                    index = (index + 1 > length - 1) ? (length - 1) : (index + 1);
                }

                $this.slider("option", "value", index * smoothness);
            } else if (event.keyCode >= 37 && event.keyCode <= 40) {
                $this.slider("option", "value", prevIndex * smoothness);
                event.preventDefault();
                return;
            }

            value = $this.data("labels")[index];
            updateValueLabel($label, value);
            $this.data("index", index);
            $this.data("value", value);
            $this.trigger("change", [index]);
        }

        function onSliderStop(event, ui) {
            // jshint validthis: true
            var $this = $(this);
            var index = $this.data("index");

            $this.find(".fc-slider-value-label").hide();
            $this.slider("option", "value", index * smoothness);
        }

        function updateValueLabel($label, value) {
            $label.text(value);
            $label.css("margin-left", -$label.outerWidth() / 2 + 10);
        }

        return $.extend(true, self.each(function () {
            var $this = $(this);

            $this.on("slidecreate", onSliderCreate)
                .on("slidestart", onSliderStart)
                .on("slide", onSliderSlide)
                .on("slidestop", onSliderStop)
                .slider(options);

            $this.data("labels", []);
        }), {
            setLabels: function (labels) {
                return self.each(function () {
                    var $this = $(this);
                    var min = 0;
                    var max = labels.length - 1;

                    $this.attr("data-min", labels[min])
                        .attr("data-max", labels[max])
                        .data("labels", labels)
                        .data("index", min)
                        .data("value", labels[min])
                        .slider("option", {
                            min: min * smoothness,
                            max: max * smoothness,
                            value: min * smoothness
                        });

                    $this.trigger("change", [min]);
                });
            },
            setIndex: function (index) {
                var $this = $(this);
                var value = $this.data("labels")[index];
                $this.slider("option", "value", index * smoothness);
                $this.data("index", index);
                $this.data("value", value);
                $this.trigger("change", [index]);
            }
        });
    };

    $.fn.yearSlider = function (options) {
        var self = this;
        var baseSetLabels;
        var baseSetIndex;

        function onSliderSlide(event, ui) {
            // jshint validthis: true
            FC.Results.timeSlicePanel.updateSliceValue();

            $(this).trigger("slicechange", {
                type: "year",
                index: $(this).data("index")
            });
        }

        return $.extend(true, self.each(function () {
            var $this = $(this);
            $this.labeledSlider(options);
            $this.on("slide", onSliderSlide);
            baseSetLabels = $this.setLabels;
            baseSetIndex = $this.setIndex;
        }), {
            setIndex: baseSetIndex,
            setPointLabels: baseSetLabels,
            setRangeLabels: function (labels) {
                return self.each(function () {
                    var mappedLabels = [];
                    var rangeStart, rangeEnd;

                    while (labels.length > 1) {
                        rangeStart = labels.shift();
                        rangeEnd = labels[0] - 1;
                        mappedLabels.push(rangeStart + "-" + rangeEnd);
                    }

                    baseSetLabels(mappedLabels);
                });
            },
            setSingleLabel: function (labels) {
                var label;
                var isPoint = labels.length === 1 || (labels.length === 2 && labels[0] === labels[1]);
                var isRange = labels.length === 2 && labels[0] !== labels[1];

                if (isRange) {
                    label = labels[0] + "-" + labels[1];
                } else if (isPoint) {
                    label = labels[0];
                }

                baseSetLabels([label]);
            }
        });
    };

    $.fn.daySlider = function (options) {
        var self = this;
        var baseSetLabels;
        var baseSetIndex;

        function onSliderSlide(event, ui) {
            // jshint validthis: true
            FC.Results.timeSlicePanel.updateSliceValue();

            $(this).trigger("slicechange", {
                type: "day",
                index: $(this).data("index")
            });
        }

        function getDayWithMonth(day, isLeapYear) {
            var monthName, mn;
            var months = {
                January: 31, February: isLeapYear ? 29 : 28, March: 31, April: 30,
                May: 31, June: 30, July: 31, August: 31,
                September: 30, October: 31, November: 30, December: 31
            };

            for (mn in months) {
                if (day > months[mn]) {
                    day -= months[mn];
                } else {
                    monthName = mn;
                    break;
                }
            }

            // Add ordinal indicator.
            switch (day) {
            case 1:
            case 21:
            case 31:
                day += "st";
                break;
            case 2:
            case 22:
                day += "nd";
                break;
            case 3:
            case 23:
                day += "rd";
                break;
            default:
                day += "th";
                break;
            }

            return { day: day, month: monthName };
        }

        return $.extend(true, self.each(function () {
            var $this = $(this);
            $this.labeledSlider(options);
            $this.on("slide", onSliderSlide);
            baseSetLabels = $this.setLabels;
            baseSetIndex = $this.setIndex;
        }), {
            setIndex: baseSetIndex,
            setPointLabels: function (labels, isLeapYear) {
                return self.each(function () {
                    var mappedLabels = [];

                    mappedLabels = labels.map(function (label) {
                        label = getDayWithMonth(label, isLeapYear);
                        return label.day + " " + label.month;
                    });

                    baseSetLabels(mappedLabels);
                });
            },
            setRangeLabels: function (labels, isLeapYear) {
                return self.each(function () {
                    var mappedLabels = [];
                    var rangeStart, rangeEnd;

                    if (FC.isMonthlyCellAxis(labels)) {
                        while (labels.length > 1) {
                            labels.shift();
                            rangeEnd = getDayWithMonth(labels[0] - 1, isLeapYear);
                            mappedLabels.push(rangeEnd.month);
                        }
                    } else {
                        while (labels.length > 1) {
                            rangeStart = getDayWithMonth(labels.shift(), isLeapYear);
                            rangeEnd = getDayWithMonth(labels[0] - 1, isLeapYear);
                            rangeStart = rangeStart.day + " " + rangeStart.month;
                            rangeEnd = rangeEnd.day + " " + rangeEnd.month;
                            mappedLabels.push(rangeStart + "-" + rangeEnd);
                        }
                    }

                    baseSetLabels(mappedLabels);
                });
            },
            setSingleLabel: function (labels, isLeapYear) {
                var label;
                var rangeStart, rangeEnd;
                var isPoint = labels.length === 1 || (labels.length === 2 && labels[0] === labels[1]);
                var isRange = labels.length === 2 && labels[0] !== labels[1];

                if (isRange) {
                    rangeStart = getDayWithMonth(labels[0], isLeapYear);
                    rangeEnd = getDayWithMonth(labels[1] - 1, isLeapYear);
                    rangeStart = rangeStart.day + " " + rangeStart.month;
                    rangeEnd = rangeEnd.day + " " + rangeEnd.month;
                    label = rangeStart + "-" + rangeEnd;
                } else if (isPoint) {
                    label = getDayWithMonth(labels[0], isLeapYear);
                    label = label.day + " " + label.month;
                }

                baseSetLabels([label]);
            }
        });
    };

    $.fn.hourSlider = function (options) {
        var self = this;
        var baseSetLabels;
        var baseSetIndex;

        function onSliderSlide(event, ui) {
            // jshint validthis: true
            FC.Results.timeSlicePanel.updateSliceValue();

            $(this).trigger("slicechange", {
                type: "hour",
                index: $(this).data("index")
            });
        }

        function getHourWithMinutes(hour) {
            return hour + ":00";
        }

        return $.extend(true, self.each(function () {
            var $this = $(this);
            $this.labeledSlider(options);
            $this.on("slide", onSliderSlide);
            baseSetLabels = $this.setLabels;
            baseSetIndex = $this.setIndex;
        }), {
            setIndex: baseSetIndex,
            setPointLabels: function (labels) {
                return self.each(function () {
                    var mappedLabels = [];

                    mappedLabels = labels.map(function (label) {
                        return getHourWithMinutes(label);
                    });

                    baseSetLabels(mappedLabels);
                });
            },
            setRangeLabels: function (labels) {
                return self.each(function () {
                    var mappedLabels = [];
                    var rangeStart, rangeEnd;

                    while (labels.length > 1) {
                        rangeStart = getHourWithMinutes(labels.shift());
                        rangeEnd = getHourWithMinutes(labels[0] - 1);
                        mappedLabels.push(rangeStart + "-" + rangeEnd);
                    }

                    baseSetLabels(mappedLabels);
                });
            },
            setSingleLabel: function (labels) {
                var label;
                var isPoint = labels.length === 1 || (labels.length === 2 && labels[0] === labels[1]);
                var isRange = labels.length === 2 && labels[0] !== labels[1];

                if (isRange) {
                    label = getHourWithMinutes(labels[0]) + "-" + getHourWithMinutes(labels[1] - 1);
                } else if (isPoint) {
                    label = getHourWithMinutes(labels[0]);
                }

                baseSetLabels([label]);
            }
        });
    };

    $.fn.dropdown = function () {
        var self = this;

        function onDropdownClick(event) {
            // jshint validthis: true
            var $this = $(this);
            var $opts = $this.find("ul.fc-dropdown > li");
            $this.toggleClass("active");
            $opts.toggle();
            event.stopPropagation();
        }

        function onOptionClick(event) {
            // jshint validthis: true
            var $this = event.data;
            var $span = $this.find("span");
            var $opts = $this.find("ul.fc-dropdown > li");
            var $opt = $(this);
            var index = $opt.index();
            var optText = $opt.text();

            $span.text(optText);
            $this.data("index", index);
            $this.data("option", optText);

            if (index === 0) {
                $span.addClass("default");
            } else {
                $span.removeClass("default");
            }

            $this.trigger("change", [ optText ]);
            $this.removeClass("active");
            $opts.hide();

            event.stopPropagation();
        }

        return $.extend(true, self.each(function () {
            var $this = $(this);
            var $ul = $this.find(".fc-dropdown");
            var $span = $this.find("span");
            var $opts = $this.find("ul.fc-dropdown > li");
            var optText = $opts.first().text();

            $span.text(optText);
            $this.data("index", 0);
            $this.data("option", optText);
            $this.trigger("change", [ optText ]);
            $span.addClass("default");

            $this.click(onDropdownClick);
            $opts.click($this, onOptionClick);
        }), {
            selectOption: function (index) {
                return self.each(function () {
                    var $this = $(this);
                    var $opts = $this.find("ul.fc-dropdown > li");
                    $($opts.get(index)).click();
                });
            },

            selectOptionByText: function (text) {
                return self.each(function () {
                    var $this = $(this);
                    var $opts = $this.find("ul.fc-dropdown > li");
                    var optTexts = $opts.map(function (i, opt) {
                        return $(opt).text();
                    }).get();
                    var index = Math.max(optTexts.indexOf(text), 0);
                    $($opts.get(index)).click();
                });
            },

            setOptions: function (options) {
                return self.each(function () {
                    var $this = $(this);
                    var $ul = $this.find(".fc-dropdown");
                    var $opts;

                    $this.css("visibility", "visible");
                    $ul.children().remove();
                    options.forEach(function (opt) {
                        $("<li></li>").text(opt).appendTo($ul);
                    });

                    $opts = $this.find("ul.fc-dropdown > li");
                    $this.off("click").click(onDropdownClick);
                    $opts.off("click").click($this, onOptionClick);

                    if (options.length < 1) {
                        $this.css("visibility", "hidden");
                    }
                });
            }
        });
    };

    $.fn.paletteDropdown = function () {
        var self = this;

        return $.extend(true, self.each(function () {
            var $this = $(this);
            var $ul = $this.find(".fc-dropdown");
            var $opts = $this.find("ul.fc-dropdown > li");
            var $paletteControls = $this.find(".fc-palette-control");

            // Store initial continuous palettes in jQuery control.
            $this.data("palette-controls", []);
            $this.data("palettes", []);

            var initializePalettes = function () {
                $paletteControls.each(function (i, control) {
                    var $paletteControl = $(control);
                    var paletteString = $(control).attr("data-palette");
                    var palette = D3.ColorPalette.parse(paletteString);
                    var paletteControl = new D3.ColorPaletteViewer($paletteControl, palette, {
                        height: FC.Settings.PALETTE_HEIGHT,
                        axisVisible: false
                    });

                    $paletteControl.data("palette-control", paletteControl);
                    $paletteControl.data("palette", palette);
                    $this.data("palette-controls").push(paletteControl);
                    $this.data("palettes").push(palette);
                });
            };

            var initialize = function () {
                $this.css("visibility", "hidden").click();

                var watch = function () {
                    if ($opts.is(":visible")) {
                        initializePalettes();
                        $this.data("discrete", false);
                        $this.data("bands", FC.Settings.DEFAULT_PALETTE_BANDS);
                        $this.click().css("visibility", "visible");
                    } else {
                        setTimeout(watch, 50);
                    }
                };

                setTimeout(watch, 50);
            };

            $this.click(function (event) {
                $this.toggleClass("active");
                $opts.toggle();
                event.stopPropagation();
            });

            $opts.click(function (event) {
                var $paletteControl = $(this).find(".fc-palette-control");
                var palette = $paletteControl.data("palette");

                $paletteControls.removeAttr("data-selected");
                $paletteControl.attr("data-selected", true);
                $this.trigger("change", [ palette ]);
                $this.removeClass("active");
                $opts.hide();

                event.stopPropagation();
            });

            initialize();
        }), {
            setBands: function (bands) {
                return self.each(function () {
                    var $this = $(this);
                    $this.data("bands", bands);
                    if ($this.data("discrete")) {
                        self.setContinuousPalettes(true);
                        self.setDiscretePalettes();
                    }
                });
            },
            setDiscretePalettes: function (noUpdate) {
                return self.each(function () {
                    var $this = $(this);
                    var $paletteControls = $this.find(".fc-palette-control");
                    var bands = $this.data("bands");

                    $this.data("discrete", true);
                    $this.data("palette-controls").forEach(function (paletteControl, i) {
                        var $paletteControl = $paletteControls.eq(i);
                        var palette = paletteControl.palette.banded(bands);
                        paletteControl.palette = palette;
                        $paletteControl.data("palette-control", paletteControl);
                        $paletteControl.data("palette", palette);

                        if ($paletteControl.attr("data-selected") && !noUpdate) {
                            $this.trigger("change", [ palette ]);
                        }
                    });
                });
            },
            setContinuousPalettes: function (noUpdate) {
                return self.each(function () {
                    var $this = $(this);
                    var $paletteControls = $this.find(".fc-palette-control");

                    $this.data("discrete", false);
                    $this.data("palette-controls").forEach(function (paletteControl, i) {
                        var $paletteControl = $paletteControls.eq(i);
                        var paletteString = $paletteControl.attr("data-palette");
                        var palette = D3.ColorPalette.parse(paletteString);
                        paletteControl.palette = palette;
                        $paletteControl.data("palette-control", paletteControl);
                        $paletteControl.data("palette", palette);

                        if ($paletteControl.attr("data-selected") && !noUpdate) {
                            $this.trigger("change", [ palette ]);
                        }
                    });
                });
            }
        });
    };

})(jQuery);