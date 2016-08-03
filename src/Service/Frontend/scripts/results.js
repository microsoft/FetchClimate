var FC = (function (FC, window, $) {
    "use strict";
    FC.Results = {};

    var _$page,
        _resultsPanel,
        _timeSlicePanel;

    Object.defineProperties(FC.Results, {
        $page: { get: function () { return _$page; } },
        resultsPanel: { get: function () { return _resultsPanel; } },
        timeSlicePanel: { get: function () { return _timeSlicePanel; } }
    });

    function fetchMissingDataCallback(request) {
        var variable = request.variable;
        var status = FC.state.getVariableStatus(variable);
        var tile = _resultsPanel.getTile(variable);

        console.log("[results] callback:", variable, status);

        if (/completed/gi.test(status)) {
            tile.info = status;
            tile.inProgress = false;
        }

        if (/queued|progress|receiving/gi.test(status)) {
            tile.info = status;
            tile.inProgress = true;
        }

        if (/failed/gi.test(status)) {
            var firstHash,
                url = FC.Settings.FETCHCLIMATE_2_SERVICE_URL + "/logs";

            if (status.split(" ").length > 1) {
                firstHash = status.split(" ")[1].split(",")[0];
            }

            if (firstHash.length > 0) {
                url += "/" + firstHash;
            }

            status = "Failed, <a class='error-log-link' href='" + url + "'>please see logs</a>";

            tile.info = status;
            tile.inProgress = false;
        }

        if (/unexpected status/gi.test(status)) {
            tile.info = status;
            tile.inProgress = false;
        }

        if (FC.state.selectedVariable === variable) {
            FC.Results.onDataModeChange();
            FC.Results.updateTimeChartGraph();
        }
    }

    function getDataSourceName(id) {
        var dataSource = FC.state.config.DataSources.filter(function (ds) {
            return ds.ID === id;
        })[0];

        return dataSource ? dataSource.Name : null;
    }

    function onSelectedVariableChange (event, data) {
        data = event.data || data;

        FC.Results.updatePalette();
        FC.Results.updateProbe();
        FC.Results.onDataModeChange();
        FC.Results.updateTimeChartFrame();
        FC.Results.updateTimeChartGraph();
    }

    function onSliceChange (event, data) {
        data = event.data || data;

        FC.Map.drawVariableData(FC.state.selectedVariable);
        FC.Results.updateProbe();
        _resultsPanel.timeChartControl.updateSliceLine();
    }

    function onPaletteChange (event, data) {
        data = event.data || data;

        FC.Map.drawVariableData(FC.state.selectedVariable);
    }

    function updateMapCopyrightPosition() {
        var h = _timeSlicePanel.$panel.is(":visible") ? _timeSlicePanel.$panel.height() : 0;
        $(".OverlaysBR").css("bottom", (2*FC.Settings.TIME_SLICE_PANEL_PADDING + h) + "px");
    }

    function onShowSlicePanelChange (event, data) {
        data = event.data || data;

        if (data.isShowing) {
            _timeSlicePanel.show();
            updateMapCopyrightPosition();
        }
        else {
            _timeSlicePanel.hide();
            updateMapCopyrightPosition();
        }
    }

    $.extend(FC.Results, {
        initialize: function () {
            _$page = $("section.results");
            _resultsPanel = new FC.Controls.ResultsPanel($(".results-panel"));
            _timeSlicePanel = new FC.Controls.TimeSlicePanel($(".time-slice-panel"));

            _$page.on("selectedvariablechange", onSelectedVariableChange);
            _$page.on("slicechange", onSliceChange);
            _$page.on("palettechange", onPaletteChange);
            _$page.on("showslicepanelchange", onShowSlicePanelChange);

            FC.mapEntities.onUpdateProbe = FC.Results.onUpdateProbe;

            _resultsPanel.dataInfoControl
                .$dataModeSelector
                .change(FC.Results.onDataModeChange);

            _resultsPanel.timeChartControl
                .$timeSeriesAxisSelector
                .change(FC.Results.onTimeSeriesAxisChange);

            _resultsPanel.$detailsTab.click(function () {
                FC.Results.updateTimeChartFrame();
                FC.Results.updateTimeChartGraph();
            });

            FC.Map.mapOptionsPanel.$opacitySlider.change(function (event, index) {
                var opacity = index / 100;
                FC.Map.plot.host.css("opacity", opacity);
            });
            FC.Map.mapOptionsPanel.$opacitySlider.setIndex(FC.Settings.DEFAULT_OPACITY);

            FC.Map.updateChartOnMapLoad();
        },

        updateSection: function () {
            FC.Results.updateSliders();
            FC.Results.updateResults();
            FC.Map.updateChartOnMapLoad();

            if (_resultsPanel.$detailsTab.hasClass("active")) {
                FC.Results.updateTimeChartGraph();
            }

            FC.Results.updateTimeChartFrame();

            if (FC.Map.probeMode) {
                _resultsPanel.toggleProbeMode();
            }
        },


        updateResults: function () {
            var variable;

            
            // No grid or point selected, show 'no area selected' message.
            if (FC.state.grids.length === 0 && FC.state.points.length === 0) {
                _resultsPanel.updatePanelMessage(FC.Settings.NO_AREA_SELECTED_MESSAGE)
                    .$panelMessage.show();
                _resultsPanel.$controls.hide();
                _resultsPanel.$content.hide();
                _timeSlicePanel.$panel.hide();
            } else if ($.isEmptyObject(FC.state.variables)) { // No layer selected, show 'no layer selected' message.
                _resultsPanel.updatePanelMessage(FC.Settings.NO_LAYERS_SELECTED_MESSAGE)
                    .$panelMessage.show();
                _resultsPanel.$controls.hide();
                _resultsPanel.$content.hide();
                _timeSlicePanel.$panel.hide();
                FC.Map.drawVariableData();
            }
            // Start fetching data, showing panel controls.
            else {
                _resultsPanel.$panelMessage.hide();
                _resultsPanel.$controls.show();
                _resultsPanel.$content.show();
                _timeSlicePanel.$panel.show();

                if (FC.state.anyPendingRequest()) {
                    _resultsPanel.clearTiles();

                    FC.state.fetchMissingData(fetchMissingDataCallback);

                    for (variable in FC.state.variables) {
                        _resultsPanel.addTile(variable);
                        fetchMissingDataCallback({ variable: variable });
                    }
                } else {
                    _resultsPanel.$tiles.children().each(function (i, tile) {
                        variable = $(tile).attr("data-variable");
                        if (!FC.state.variables[variable]) {
                            _resultsPanel.removeTile(variable);
                        } else {
                            fetchMissingDataCallback({ variable: variable });
                        }
                    });
                }

                if (!FC.state.selectedVariable) {
                    _resultsPanel.selectFirstTile();
                }
                else {
                    _resultsPanel.selectTile(FC.state.selectedVariable);
                }
            }

            updateMapCopyrightPosition();
            _resultsPanel.updateTabsHeight();
        },

        updateSliders: function () {
            if (_timeSlicePanel) _timeSlicePanel.updateSliders();
        },

        updatePalette: function () {
            var dataInfoControl = _resultsPanel.dataInfoControl;
            var range = FC.state.getVariableDataRange();
            var units = FC.state.getVariableUnits();

            if (isNaN(range.min) || isNaN(range.max)) {
                dataInfoControl.hidePaletteAxis();
                dataInfoControl.hideUnits();
            } else {
                dataInfoControl.setPaletteRange(range);
                dataInfoControl.showPaletteAxis();
                dataInfoControl.setUnits(units);
                dataInfoControl.showUnits();
            }
        },

        updateProvenance: function () {
            var dataCube;
            var palette = FC.state.palette;
            var points = [];
            var provInfo = [];

            FC.state.grids.forEach(function (grid) {
                dataCube = FC.state.getGridDataCube(grid);
                if (dataCube) {
                    FC.appendUniqueProvenanceIDs(dataCube, points);
                }
            });

            dataCube = FC.state.getPointDataCube();
            if (dataCube) {
                FC.appendUniqueProvenanceIDs(dataCube, points);
            }

            if (points.length == 1) {
                palette = palette.absolute(0, Math.max(points[0], 0.0001));
            } else if (points.length > 1) {
                palette = palette.absolute(Math.min.apply(Math, points), Math.max.apply(Math, points) + 0.0001);
            }

            FC.state.setPalette(palette);

            points.forEach(function (point) {
                var color = palette.getRgba(point);
                var name = getDataSourceName(point);

                if (name) {
                    provInfo.push({
                        color: "rgba(" + color.r + "," + color.g + "," + color.b + "," + color.a + ")",
                        name: getDataSourceName(point)
                    });
                }
            });

            if (provInfo.length > 0) {
                _resultsPanel.dataInfoControl.setProvenance(provInfo);
            }
        },

        updateTimeChartFrame: function () {
            var timeChartControl = _resultsPanel.timeChartControl;

            setTimeout(function () {
                if ((FC.state.yearSlice !== null || FC.state.daySlice !== null ||
                    FC.state.hourSlice !== null) && FC.state.dataMode !== "provenance" &&
                    !$.isEmptyObject(FC.state.variables) && FC.state.hasAnyCompletedData() &&
                    !FC.state.hasAllNaNData()) {
                    timeChartControl.show();
                    timeChartControl.updateTimeSeriesSelector();
                    timeChartControl.updateHorizontalAxis();
                    timeChartControl.updateAxisLabels();
                } else {
                    timeChartControl.hide();
                }

                _resultsPanel.updateTabsHeight();
            }, 0);
        },

        updateTimeChartGraph: function () {
            var timeChartControl = _resultsPanel.timeChartControl;

            setTimeout(function () {
                if ((FC.state.yearSlice !== null || FC.state.daySlice !== null ||
                    FC.state.hourSlice !== null) && FC.state.dataMode !== "provenance" &&
                    !$.isEmptyObject(FC.state.variables) && FC.state.hasAnyCompletedData() &&
                    !FC.state.hasAllNaNData()) {
                    timeChartControl.show();
                    timeChartControl.updateMainAreaPlot();
                    timeChartControl.updateSliceLine();
                } else {
                    timeChartControl.hide();
                }

                _resultsPanel.updateTabsHeight();
            }, 0);
        },

        updateProbe: function () {
            var value,
                location;

            if (FC.Map.probeMode && FC.mapEntities.probe.getVisible()) {
                location = FC.mapEntities.probe.getLocation();
                value = FC.Map.getValueByLocation(location);

                if (value !== null && typeof value !== undefined) {
                    FC.mapEntities.updateProbe({
                        value: value
                    });
                }
                else {
                    FC.Map.hideProbe();
                    _resultsPanel.timeChartControl.clearProbePolyline();
                }
            }
        },

        onMapEntityHover: function (event) {
            if (!_resultsPanel || !_resultsPanel.dataInfoControl || !_resultsPanel.dataInfoControl.paletteControl) return;

            var dataInfoControl = _resultsPanel.dataInfoControl;
            var timeChartControl = _resultsPanel.timeChartControl;
            var map = event.data;
            var sx = event.offsetX;
            var sy = event.offsetY;
            var hm, ct, dt, sdt, v;
            var px, py, dx, dy;
            var point, grid, tooltip;
            var isEntityHovered;

            FC.state.grids.some(function (grid) {
                hm = grid.heatmap;
                if (hm) {
                    ct = hm.coordinateTransform;
                    dt = hm.yDataTransform;
                    px = ct.screenToPlotX(sx);
                    py = ct.screenToPlotY(sy);
                    dx = px;
                    dy = dt.plotToData(py);
                    v = hm.getValue(dx, dy);
                }
                return !!v;
            });

            if (FC.Map.markersPlot) {
                if (!ct || !dt) {
                    ct = FC.Map.markersPlot.coordinateTransform;
                    dt = FC.Map.markersPlot.yDataTransform;
                    px = ct.screenToPlotX(sx);
                    py = ct.screenToPlotY(sy);
                    dx = px;
                    dy = dt.plotToData(py);
                }

                tooltip = FC.Map.markersPlot.findToolTipMarkers(dx, dy, px, py)[0];
            }

            if (tooltip) {
                dx = tooltip.x;
                dy = tooltip.y;
                v = tooltip.value;
            }
            
            if (v) {
                dataInfoControl.showMarker();
                dataInfoControl.setMarker(v);
            } else {
                dataInfoControl.hideMarker();
            }

            if (v && FC.state.dataMode !== "provenance") {
                dataInfoControl.showValue();
                dataInfoControl.setValue(v);
            }
            else {
                dataInfoControl.hideValue();
            }

            // Draw polyline for a point on hover.
            if (FC.Map.markersPlot) {
                isEntityHovered = FC.state.points.some(function (point) {
                    var isHovered = false;
                    var psx, psy;
                    
                    ct = FC.Map.markersPlot.coordinateTransform;
                    dt = FC.Map.markersPlot.yDataTransform;
                    px = point.lon;
                    py = dt.dataToPlot(point.lat);
                    psx = ct.plotToScreenX(px);
                    psy = ct.plotToScreenY(py);
                    isHovered = Math.abs(psx - sx) < FC.Settings.DEFAULT_MARKER_SIZE &&
                                Math.abs(psy - sy) < FC.Settings.DEFAULT_MARKER_SIZE;

                    if (isHovered) {
                        timeChartControl.updatePointHoverPolyline(point);
                    }

                    return isHovered;
                });

                if (!isEntityHovered) {
                    timeChartControl.clearHoverPolyline();
                }
            }

            // Draw polyline for a grid cell on hover.
            if (!isEntityHovered) {
                isEntityHovered = FC.state.grids.some(function (grid) {
                    var isHovered = false;
                    var latGrid, lonGrid;
                    var k, lat, lon;

                    hm = grid.heatmap;

                    if (hm) {
                        latGrid = grid.getLatGrid();
                        lonGrid = grid.getLonGrid();
                        ct = hm.coordinateTransform;
                        sdt = hm.getScreenToDataTransform();
                        dx = sdt.screenToDataX(sx);
                        dy = sdt.screenToDataY(sy);

                        if (grid.latmin <= dy && dy <= grid.latmax && grid.lonmin <= dx && dx <= grid.lonmax) {
                            isHovered = true;

                            for (k = 0; k < latGrid.length && latGrid[k] < dy; k++);
                            lat = Math.max(0, Math.min(latGrid.length - 1, k - 1));

                            for (k = 0; k < lonGrid.length && lonGrid[k] < dx; k++);
                            lon = Math.max(0, Math.min(lonGrid.length - 1, k - 1));

                            timeChartControl.updateGridHoverPolyline(grid, lat, lon);
                        }
                    }
                    
                    return isHovered;
                });

                if (!isEntityHovered) {
                    timeChartControl.clearHoverPolyline();
                }
            }
        },

        onDataModeChange: function (event, dataMode) {
            if (!_resultsPanel || !_resultsPanel.dataInfoControl || !_resultsPanel.dataInfoControl.paletteControl) return;

            var dataInfoControl = _resultsPanel.dataInfoControl;
            var timeChartControl = _resultsPanel.timeChartControl;
            var $paletteControl = dataInfoControl.$paletteControl;
            var $provenanceControl = dataInfoControl.$provenanceControl;

            dataMode = dataMode || FC.state.dataMode;
            dataMode = dataMode.toLowerCase();

            if (dataMode === "provenance") {
                $paletteControl.hide();
                $provenanceControl.show();
                dataInfoControl.hideUnits();
                FC.Results.updateProvenance();
                timeChartControl.hide();
                dataInfoControl.hideValue();
            } else {
                $provenanceControl.hide();
                $paletteControl.show();
                dataInfoControl.showUnits();
                FC.Results.updatePalette();
                timeChartControl.show();
            }

            FC.Map.drawVariableData();
            FC.Results.updateProbe();
            FC.Results.updateTimeChartFrame();
            FC.Results.updateTimeChartGraph();
        },

        onTimeSeriesAxisChange: function (event, timeSeriesAxis) {
            var timeChartControl = _resultsPanel.timeChartControl;

            setTimeout(function () {
                timeChartControl.updateHorizontalAxis();
                timeChartControl.updateAxisLabels();
                timeChartControl.updateMainAreaPlot();
                timeChartControl.updateSliceLine();
                FC.Results.onUpdateProbe();
            }, 0);
        },

        onUpdateProbe: function (dy, dx) {
            var timeChartControl = _resultsPanel.timeChartControl;
            var isEntityHovered = false;
            var probeLoc;

            dx = +dx;
            dy = +dy;

            if (!FC.mapEntities.probe.getVisible()) {
                timeChartControl.clearProbePolyline();
                return;
            }

            if (!dx || !dy) {
                probeLoc = FC.mapEntities.probe.getLocation();
                dx = probeLoc.longitude;
                dy = probeLoc.latitude;
            }

            if (FC.Map.markersPlot) {
                isEntityHovered = FC.state.points.some(function (point) {
                    var isHovered = false;
                    var ct, dt;
                    var px, py, sx, sy;
                    var ppx, ppy, psx, psy;

                    ct = FC.Map.markersPlot.coordinateTransform;
                    dt = FC.Map.markersPlot.yDataTransform;
                    px = dx;
                    py = dt.dataToPlot(dy);
                    sx = ct.plotToScreenX(px);
                    sy = ct.plotToScreenY(py);
                    ppx = point.lon;
                    ppy = dt.dataToPlot(point.lat);
                    psx = ct.plotToScreenX(ppx);
                    psy = ct.plotToScreenY(ppy);
                    isHovered = Math.abs(psx - sx) < FC.Settings.DEFAULT_MARKER_SIZE &&
                                Math.abs(psy - sy) < FC.Settings.DEFAULT_MARKER_SIZE;

                    if (isHovered) {
                        timeChartControl.updatePointProbePolyline(point);
                    }

                    return isHovered;
                });
            }

            if (!isEntityHovered) {
                isEntityHovered = FC.state.grids.some(function (grid) {
                    var hm = grid.heatmap;
                    var isHovered = false;
                    var latGrid, lonGrid;
                    var k, lat, lon;

                    if (hm) {
                        latGrid = grid.getLatGrid();
                        lonGrid = grid.getLonGrid();

                        if (grid.latmin <= dy && dy <= grid.latmax && grid.lonmin <= dx && dx <= grid.lonmax) {
                            isHovered = true;

                            for (k = 0; k < latGrid.length && latGrid[k] < dy; k++);
                            lat = Math.max(0, Math.min(latGrid.length - 1, k - 1));

                            for (k = 0; k < lonGrid.length && lonGrid[k] < dx; k++);
                            lon = Math.max(0, Math.min(lonGrid.length - 1, k - 1));

                            timeChartControl.updateGridProbePolyline(grid, lat, lon);
                        }
                    }

                    return isHovered;
                });

                if (!isEntityHovered) {
                    timeChartControl.clearProbePolyline();
                }
            }
        }
    });

    return FC;
}(FC || {}, window, jQuery));