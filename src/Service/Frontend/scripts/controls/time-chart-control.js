(function (FC, $) {
    (function (Controls) {
        "use strict";

        Controls.TimeChartControl = function (source) {
            var self = this;

            var _$control = source;
            var _$timeSeriesAxisSelector = _$control.find(".timeseries-axis-selector").dropdown();
            var _$zoomControls = _$control.find(".zoom-controls");
            var _$timeChart = _$control.find("div[data-d3-plot=figure]");
            var _$leftTitle = _$control.find(".left-title");
            var _$bottomTitle = _$control.find(".bottom-title");
            var _timeChart = D3.asPlot(_$timeChart);
            var _horizontalAxis = null;
            var _polylineHover = null;
            var _polylineProbe = null;
            var _polylineMin = null;
            var _polylineMax = null;
            var _areaPlot = null;
            var _uncHoverMax = null;
            var _uncHoverMin = null;

            Object.defineProperties(self, {
                $control: { get: function () { return _$control; } },
                $timeSeriesAxisSelector: { get: function () { return _$timeSeriesAxisSelector; } },
                $zoomControls: { get: function () { return _$zoomControls; } },
                $timeChart: { get: function () { return _$timeChart; } },
                $leftTitle: { get: function () { return _$leftTitle; } },
                $bottomTitle: { get: function () { return _$bottomTitle; } },
                timeSeriesAxis: { get: function () { return _$timeSeriesAxisSelector.data("option").toLowerCase(); } },
                leftTitle: { get: function () { return _$leftTitle.text(); } },
                bottomTitle: { get: function () { return _$bottomTitle.text(); } },
                timeChart: { get: function () { return _timeChart; } }
            });

            function initialize() {
                var gestureSource = D3.Gestures.getGesturesStream(_timeChart.host);
                _timeChart.navigation.gestureSource = gestureSource;
                initializeZoomControls();
            }

            function initializeZoomControls() {
                var gestureSource = D3.Gestures.getGesturesStream(_timeChart.host);

                var zoomInClicks = _$zoomControls.find(".in")
                    .clickAsObservable()
                    .zip(function (e) {
                        var delta = 1.0 / D3.Gestures.zoomLevelFactor;
                        var centralPart = _timeChart.centralPart;
                        var halfWidth = centralPart.width() / 2;
                        var halfHeight = centralPart.height() / 2;
                        var x = halfWidth;
                        var y = halfHeight;

                        return new D3.Gestures.ZoomGesture(x, y, delta, "Mouse");
                    });

                var zoomOutClicks = _$zoomControls.find(".out")
                    .clickAsObservable()
                    .zip(function (e) {
                        var delta = 1.0 * D3.Gestures.zoomLevelFactor;
                        var centralPart = _timeChart.centralPart;
                        var halfWidth = centralPart.width() / 2;
                        var halfHeight = centralPart.height() / 2;
                        var x = halfWidth;
                        var y = halfHeight;

                        return new D3.Gestures.ZoomGesture(x, y, delta, "Mouse");
                    });

                _timeChart.navigation.gestureSource = gestureSource.merge(zoomInClicks).merge(zoomOutClicks);
            }

            function getLeftLabel() {
                return FC.state.getVariableUnits();
            }

            function getBottomLabel() {
                return _$timeSeriesAxisSelector.data("option");
            }

            self.show = function () {
                _$control.show();
            };

            self.hide = function () {
                _$control.hide();
            };

            self.updateTimeSeriesSelector = function () {
                var temporal = FC.state.temporal;
                var tsa = FC.state.timeSeriesAxis;
                var options = [];
                var curOption = tsa.charAt(0).toUpperCase() + tsa.slice(1);

                if (temporal.yearAxisLength() > 1) options.push("Years");
                if (temporal.dayAxisLength() > 1) options.push("Days");
                if (temporal.hourAxisLength() > 1) options.push("Hours");

                _$timeSeriesAxisSelector.setOptions(options);
                _$timeSeriesAxisSelector.selectOptionByText(curOption);
            };

            self.updateHorizontalAxis = function () {
                var labels = FC.getTimeAxisLabels(FC.state.temporal, FC.state.timeSeriesAxis);
                var bottomTitleHtml;
                var grid;

                if (_horizontalAxis) {
                    _horizontalAxis.remove();
                }

                if (labels && labels.length > 0) {
                    bottomTitleHtml = _$bottomTitle[0].outerHTML;
                    _$bottomTitle.parent().remove();

                    _timeChart.addAxis("bottom", "labels", { labels: labels });
                    grid = _timeChart.get("grid");
                    _horizontalAxis = grid.xAxis = _timeChart.getAxes("bottom")[0];

                    _timeChart.addDiv(bottomTitleHtml, "bottom");
                    _$bottomTitle = _$timeChart.find(".bottom-title");
                }
            };

            self.clearAxisLabels = function () {
                _$leftTitle.text("");
                _$bottomTitle.text("");
            };

            self.updateAxisLabels = function (state) {
                _$leftTitle.text(getLeftLabel());
                _$bottomTitle.text(getBottomLabel());
            };

            self.clearSliceLine = function () {
                timeChart.get("slice-line").highlightX = undefined;
            };

            self.updateSliceLine = function () {
                var temporal = FC.state.temporal;
                var sliceValue;
                var ys = FC.state.yearSlice || 0;
                var ds = FC.state.daySlice || 0;
                var hs = FC.state.hourSlice || 0;
                var dl = temporal.days.length - 1;
                var hl = temporal.hours.length - 1;

                switch (FC.state.timeSeriesAxis) {
                    case "years":
                        sliceValue = ys;
                        break;
                    case "days":
                        sliceValue = ds;
                        break;
                    case "hours":
                        sliceValue = hs;
                        break;
                }

                _timeChart.get("slice-line").highlightX = sliceValue;
            };

            self.clearMainAreaPlot = function () {
                if (_polylineMin && _polylineMax && _areaPlot) {
                    _polylineMin.remove();
                    _polylineMax.remove();
                    _areaPlot.remove();
                }
            };

            self.updateMainAreaPlot = function () {
                // Get time series graph data for min/max area plot.
                var data = FC.state.getTimeSeriesDataForAll();

                // Clear the graph data.
                self.clearMainAreaPlot();

                if (data) {
                    _areaPlot = self.areaPlot(data.location + " (area)", {
                        x: data.x,
                        y1: data.yMax,
                        y2: data.yMin
                    });

                    _areaPlot.fill = FC.Settings.AREA_PLOT_BACKGROUND_COLOR;

                    _polylineMin = _timeChart.polyline(data.location + " (min)", {
                        x: data.x,
                        y: data.yMin
                    });

                    _polylineMin.thickness = 2;
                    _polylineMin.stroke = FC.Settings.AREA_PLOT_FOREGROUND_COLOR;
                    _polylineMin.color = FC.Settings.AREA_PLOT_FOREGROUND_COLOR;
                    _polylineMin.host.css("z-index", 99);

                    _polylineMax = _timeChart.polyline(data.location + " (max)", {
                        x: data.x,
                        y: data.yMax
                    });

                    _polylineMax.thickness = 2;
                    _polylineMax.stroke = FC.Settings.AREA_PLOT_FOREGROUND_COLOR;
                    _polylineMax.color = FC.Settings.AREA_PLOT_FOREGROUND_COLOR;
                    _polylineMax.host.css("z-index", 99);
                }

                _timeChart.fitToView();
            };

            self.clearHoverPolyline = function () {
                if (_polylineHover) {
                    _polylineHover.remove();
                    _uncHoverMax.remove();
                    _uncHoverMin.remove();
                }
            };

            self.updatePointHoverPolyline = function (point) {
                // Get time series graph data of hovered point.
                var data = FC.state.getTimeSeriesDataForPoint(point, undefined);

                // Clear the graph data.
                self.clearHoverPolyline();

                if (data) {
                    _polylineHover = _timeChart.polyline(data.location + " (hover)", {
                        x: data.x,
                        y: data.y
                    });

                    _polylineHover.thickness = 2;
                    _polylineHover.stroke = FC.Settings.HOVERED_POINT_POLYLINE_COLOR;
                    _polylineHover.color = FC.Settings.HOVERED_POINT_POLYLINE_COLOR;
                    _polylineHover.host.css("z-index", 99);
                }

                if (FC.state.dataMode == "values") {
                    var uncdata = FC.state.getTimeSeriesDataForPoint(point, "uncertainty");
                    polylinesUncertaintyHover(uncdata, data);
                }
            };

            self.updateGridHoverPolyline = function (grid, lat, lon) {
                // Get time series graph data of hovered grid cell.
                var data = FC.state.getTimeSeriesDataForGridPoint(grid, lat, lon, undefined);

                // Clear the graph data.
                self.clearHoverPolyline();

                if (data) {
                    _polylineHover = _timeChart.polyline(data.location + " (hover)", {
                        x: data.x,
                        y: data.y
                    });

                    _polylineHover.thickness = 2;
                    _polylineHover.stroke = FC.Settings.HOVERED_POINT_POLYLINE_COLOR;
                    _polylineHover.color = FC.Settings.HOVERED_POINT_POLYLINE_COLOR;
                    _polylineHover.host.css("z-index", 99);
                }

                if (FC.state.dataMode == "values") {
                    var uncdata = FC.state.getTimeSeriesDataForGridPoint(grid, lat, lon, "uncertainty");
                    polylinesUncertaintyHover(uncdata, data);
                }
            };

            function polylinesUncertaintyHover(uncdata, data) {
                
                var unc = (function () {
                    var arr = { maxy: [], miny: [] };
                    for (var i = 0, l = data.y.length; i < l; i++) {
                        arr.maxy.push(data.y[i] + uncdata.y[i]);
                        arr.miny.push(data.y[i] - uncdata.y[i]);
                    }
                    return arr;
                })();


                if (unc) {
                    _uncHoverMax = _timeChart.polyline(data.location + " (hovermax)", {
                        x: data.x,
                        y: unc.maxy
                    });

                    _uncHoverMax.thickness = 2;
                    _uncHoverMax.stroke = FC.Settings.HOVERED_POINT_POLYLINE_COLOR;
                    _uncHoverMax.color = FC.Settings.HOVERED_POINT_POLYLINE_COLOR;
                    _uncHoverMax.host.css("z-index", 99);

                    _uncHoverMin = _timeChart.polyline(data.location + " (hovermin)", {
                        x: data.x,
                        y: unc.miny
                    });

                    _uncHoverMin.thickness = 2;
                    _uncHoverMin.stroke = FC.Settings.HOVERED_POINT_POLYLINE_COLOR;
                    _uncHoverMin.color = FC.Settings.HOVERED_POINT_POLYLINE_COLOR;
                    _uncHoverMin.host.css("z-index", 99);
                }

            };

            self.clearProbePolyline = function () {
                if (_polylineProbe) {
                    _polylineProbe.remove();
                }
            };

            self.updatePointProbePolyline = function (point) {
                // Get time series graph data of probe point.
                var data = FC.state.getTimeSeriesDataForPoint(point, undefined);

                // Clear the graph data.
                self.clearProbePolyline();

                if (data) {
                    _polylineProbe = _timeChart.polyline(data.location + " (probe)", {
                        x: data.x,
                        y: data.y
                    });

                    _polylineProbe.thickness = 2;
                    _polylineProbe.stroke = FC.Settings.PROBE_POINT_POLYLINE_COLOR;
                    _polylineProbe.color = FC.Settings.PROBE_POINT_POLYLINE_COLOR;
                    _polylineProbe.host.css("z-index", 99);
                }
            };

            self.updateGridProbePolyline = function (grid, lat, lon) {
                // Get time series graph data of probe grid cell.
                var data = FC.state.getTimeSeriesDataForGridPoint(grid, lat, lon, undefined);

                // Clear the graph data.
                self.clearProbePolyline();

                if (data) {
                    _polylineProbe = _timeChart.polyline(data.location + " (probe)", {
                        x: data.x,
                        y: data.y
                    });

                    _polylineProbe.thickness = 2;
                    _polylineProbe.stroke = FC.Settings.PROBE_POINT_POLYLINE_COLOR;
                    _polylineProbe.color = FC.Settings.PROBE_POINT_POLYLINE_COLOR;
                    _polylineProbe.host.css("z-index", 99);
                }
            };

            self.areaPlot = function (name, data) {
                var plot = _timeChart.get(name);

                if (!plot) {
                    var div = $("<div></div>", {
                        "data-d3-name": name,
                        "data-d3-plot": "areaPlot"
                    }).appendTo(_timeChart.host);
                    plot = new D3Ext.AreaPlot(div, _timeChart.master);
                    _timeChart.addChild(plot);
                }

                plot.draw(data);
                return plot;
            };

            initialize();
        };

    })(FC.Controls || (FC.Controls = {}));
})(window.FC = window.FC || {}, jQuery);