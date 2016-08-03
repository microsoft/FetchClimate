(function (FC, $) {
    (function (Controls) {
        "use strict";

        Controls.DataInfoControl = function (source) {
            var self = this;

            var _$dataInfo = typeof source !== "undefined" ? source : $("<div></div>");
            var _$dataModeSelector = _$dataInfo.find(".data-mode-selector").dropdown();
            var _$unitsLabel = _$dataInfo.find(".units-label");
            var _$paletteControl = _$dataInfo.find(".fc-palette-control");
            var _$provenanceControl = _$dataInfo.find(".fc-provenance-control");
            var _$paletteDiscreteMode = _$dataInfo.find(".palette-discrete-mode-input");
            var _$paletteBands = _$dataInfo.find(".palette-bands-input");
            var _$paletteMin = _$dataInfo.find(".palette-min-input");
            var _$paletteMax = _$dataInfo.find(".palette-max-input");
            var _paletteControl;
            var _provenanceControl;
            var _$axis;
            var _$marker;
            var _$paletteSelector;

            //value in the hovered point
            var _$valueContainer = _$dataInfo.find(".data-value-container");
            var _$value = _$dataInfo.find(".data-info-value");

            var _dataModeMap = {
                values: 0,
                uncertainty: 1,
                provenance: 2
            };

            Object.defineProperties(self, {
                $dataInfo: { get: function () { return _$dataInfo; } },
                $dataModeSelector: { get: function () { return _$dataModeSelector; } },
                $unitsLabel: { get: function () { return _$unitsLabel; } },
                $paletteControl: { get: function () { return _$paletteControl; } },
                $provenanceControl: { get: function () { return _$provenanceControl; } },
                $axis: { get: function () { return _$axis; } },
                $marker: { get: function () { return _$marker; } },
                $paletteSelector: { get: function () { return _$paletteSelector; } },
                dataMode: { get: function () { return _$dataModeSelector.data("option").toLowerCase(); } },
                units: { get: function () { return _$unitsLabel.text(); } },
                paletteControl: { get: function () { return _paletteControl; } },
                provenanceControl: { get: function () { return _provenanceControl; } },
                palette: { get: function () { return _paletteControl.palette; } },
                range: { get: function () { return _paletteControl.palette.range; } },
                provInfo: { get: function () { return _provenanceControl.provInfo; } }
            });

            function initialize() {
                var parentPage = _$dataInfo.closest("section").attr("class");

                _$paletteDiscreteMode.change(function () {
                    if ($(this).is(":checked")) {
                        _paletteControl.palette = _paletteControl.palette.banded(10);
                        _$paletteSelector.setDiscretePalettes();
                        _$paletteBands.parent().css("visibility", "visible");
                    } else {
                        _$paletteSelector.setContinuousPalettes();
                        _$paletteBands.parent().css("visibility", "hidden");
                    }
                });

                _$paletteBands.blur(function () {
                    var bands = $(this).val();
                    bands = $.isNumeric(bands) && bands > 0 ? Math.round(bands) : FC.Settings.DEFAULT_PALETTE_BANDS;
                    $(this).val(bands);
                    _$paletteSelector.setBands(bands);
                });

                _$paletteMin.add(_$paletteMax).blur(function () {
                    var palette = _paletteControl.palette;
                    var range = palette.range;
                    var min = _$paletteMin.val();
                    var max = _$paletteMax.val();

                    if (!$.isNumeric(min) || !$.isNumeric(max)) {
                        self.updateRangeInputs(range.min, range.max);
                    } else {
                        self.setPaletteWithRange(palette, { min: +min, max: +max });
                    }
                });

                _$paletteBands.val(FC.Settings.DEFAULT_PALETTE_BANDS);
                _$paletteBands.parent().css("visibility", "hidden");

                // Initialize on first page load, because visibility of containers is necessary.
                var initPalette = function () {
                    var palette = D3.ColorPalette.parse(FC.Settings.DEFAULT_PALETTE);
                    _paletteControl = new D3.ColorPaletteViewer(_$paletteControl, palette, {
                        height: FC.Settings.PALETTE_HEIGHT,
                        width: FC.Settings.PALETTE_WIDTH,
                        axisVisible: false
                    });
                    _provenanceControl = new D3Ext.ProvenancePaletteControl(_$provenanceControl);
                    _$axis = _$dataInfo.find(".d3-axis");
                    _$marker = _$dataInfo.find(".fc-palette-marker");

                    _$paletteSelector = _$dataInfo.find(".fc-palette-selector")
                        .paletteDropdown()
                        .change(function (event, palette) {
                            var range = self.range;
                            self.setPaletteWithRange(palette, range);
                        });

                    self.selectDataMode(FC.state.dataMode);
                    self.hideMarker();
                    self.hideUnits();
                };

                initPalette();
            }

            self.selectDataMode = function (dataMode) {
                if (self.dataMode === dataMode) return;
                _$dataModeSelector.selectOption(_dataModeMap[dataMode]);
            };

            self.setPaletteWithRange = function (palette, range) {
                self.updateRangeInputs(range.min, range.max);
                _paletteControl.palette = palette.absolute(range.min, range.max);

                FC.state.setPalette(_paletteControl.palette);
                _$dataInfo.trigger("palettechange", {
                    palette: _paletteControl.palette,
                });
            };

            self.setPalette = function (palette) {
                _paletteControl.palette = D3.ColorPalette.parse(palette);

                FC.state.setPalette(palette);
                _$dataInfo.trigger("palettechange", {
                    palette: palette
                });
            };

            self.setPaletteRange = function (range) {
                // min <= max always here.
                if (range.min >= range.max) {
                    range.min -= 0.5;
                    range.max += 0.5;
                }

                self.updateRangeInputs(range.min, range.max);
                _paletteControl.palette = _paletteControl.palette.absolute(range.min, range.max);

                FC.state.setPaletteRange(range);
            };

            self.setProvenance = function (provInfo) {
                _provenanceControl.provInfo = provInfo;
            };

            self.hidePaletteAxis = function () {
                _paletteControl.axisVisible = false;
                _$paletteMin.parent().hide();
                _$paletteMax.parent().hide();
            };

            self.showPaletteAxis = function () {
                _paletteControl.axisVisible = true;
                _$paletteMin.parent().show();
                _$paletteMax.parent().show();
            };

            self.hideMarker = function () {
                _$marker.hide();
            };

            self.showMarker = function () {
                _$marker.show();
            };

            self.setMarker = function (value) {
                var min = self.range.min;
                var max = self.range.max;
                var percent = 100 * (value - min) / (max - min);
                percent = (percent < 0) ? 0 : (percent > 100) ? 100 : percent;
                if (_paletteControl.axisVisible) _$marker.css("left", percent + "%");
            };

            self.showValue = function () {
                _$valueContainer.show();
            };

            self.hideValue = function () {
                _$valueContainer.hide();
            };

            self.setValue = function (value) {
                _$value.text(value.toFixed(2));
            };

            self.hideUnits = function () {
                _$unitsLabel.hide();
            };

            self.showUnits = function () {
                _$unitsLabel.show();
            };

            self.setUnits = function (units) {
                _$unitsLabel.text(units);
            };

            self.updateRangeInputs = function (min, max) {
                _$paletteMin.val(min !== null && typeof min !== "undefined" ? min.toFixed(2) : "");
                _$paletteMax.val(max !== null && typeof max !== "undefined" ? max.toFixed(2) : "");
            };

            initialize();
        };

    })(FC.Controls || (FC.Controls = {}));
})(window.FC = window.FC || {}, jQuery);