var FC = (function (FC, window, $) {
    (function (Map, window, $) {
        "use strict";

        var $geoPanel,
            $d3Div,
            $mapDiv,
            mapKey,
            plot,
            map,
            heatMapId,
            _markersPlot, // Markers plot for drawing markers (point data) on results section.
            mapSearchPanel,
            mapOptionsPanel,
            probeMode = false,
            isDragging = false;

        Object.defineProperty(Map, "plot", {
            get: function () {
                return plot;
            }
        });

        Object.defineProperty(Map, "markersPlot", {
            get: function () {
                return _markersPlot;
            },
            set: function (value) {
                _markersPlot = value;
            }
        });

        Object.defineProperty(Map, "probeMode", {
            get: function () {
                return probeMode;
            }
        });

        Object.defineProperty(Map, "mapOptionsPanel", {
            get: function () {
                return mapOptionsPanel;
            }
        });

        Map.init = function () {
            // Set visible region so map fills entire screen. NOT WORKING. Probably, bug in D3
            var pixWidth = $(window).width() - FC.Settings.NAVIGATION_PANEL_WIDTH - FC.Settings.SIDE_PANEL_WIDTH;
            var pixHeight = $(window).height();

            $geoPanel = $(".geo-panel");
            $d3Div = $("#geo-plot");
            $mapDiv = $("#geo-map");
            mapKey = $mapDiv.attr("data-d3-mapKey");
            plot = D3.asPlot($d3Div);
            map = new Microsoft.Maps.Map($mapDiv[0], {
                credentials: mapKey,
                mapTypeId: Microsoft.Maps.MapTypeId.road,
                enableClickableLogo: false,
                enableSearchLogo: false,
                showCopyright: true,
                showDashboard: false,
                showLogo: false,
                disablePanning: false,
                disableZooming: false,
                width: pixWidth,//$mapDiv.width(),
                height: pixHeight,//$mapDiv.height()
            });

            heatMapId = 0;

            mapOptionsPanel = new FC.Controls.MapOptionsPanel();
            mapSearchPanel = new FC.Controls.MapSearchPanel();

            mapOptionsPanel.appendTo($geoPanel);
            mapSearchPanel.appendTo($geoPanel);

            $d3Div.appendTo($(map.getModeLayer()));

            $(window).resize(function () {
                pixWidth = $(window).width() - FC.Settings.NAVIGATION_PANEL_WIDTH - FC.Settings.SIDE_PANEL_WIDTH;
                pixHeight = $(window).height();

                map.setOptions({ width: pixWidth, height: pixHeight });
            }).resize();

            Microsoft.Maps.registerModule("MapEntities", "scripts/map-entities.js");
            Microsoft.Maps.loadModule("MapEntities", {
                callback: function () {
                    FC.mapEntities = new FC.MapEntities(map);
                    FC.mapEntities.setMapType(Microsoft.Maps.MapTypeId.road);
                    if (FC.state) FC.Map.updateGeoPanel();
                }
            });

            Microsoft.Maps.Events.addHandler(map, 'viewchangeend', updateChart);
            Microsoft.Maps.Events.addHandler(map, 'viewchange', updateChart);

            Microsoft.Maps.Events.addHandler(map, 'mousedown', function (event) {
                if (FC.state && FC.state.activePage === "results" && probeMode) {
                    isDragging = true;
                }
            });

            Microsoft.Maps.Events.addHandler(map, 'mousemove', function (event) {
                if (FC.state && FC.state.activePage === "results" && probeMode && isDragging) {
                    updateProbe(event);
                }
            });

            Microsoft.Maps.Events.addHandler(map, 'click', function (event) {
                if (FC.state && FC.state.activePage === "results" && probeMode) {
                    updateProbe(event);
                }
            });

            Microsoft.Maps.Events.addHandler(map, 'mouseup', function (event) {
                if (FC.state && FC.state.activePage === "results" && probeMode) {
                    isDragging = false;
                }
            });

            // Update palette's marker on mouse move over heatmaps and markers.
            $mapDiv.on("mousemove", map, FC.Results.onMapEntityHover);

            map.setOptions({ width: pixWidth, height: pixHeight });
            if (pixWidth > 0 && pixHeight > 0) {
                var plotWidth = 360;
                var plotHeight = D3.mercatorTransform.dataToPlot(85) - D3.mercatorTransform.dataToPlot(-85);
                if (plotWidth * pixHeight > plotHeight * pixWidth)
                    plotWidth -= plotWidth - pixWidth * plotHeight / pixHeight;
                else
                    plotHeight -= plotHeight - plotWidth * pixHeight / pixWidth;
                plot.navigation.setVisibleRect({ x: -plotWidth / 2, y: -plotHeight / 2, width: plotWidth, height: plotHeight }, false);
            }

            mapSearchPanel.initialize();
            mapOptionsPanel.initialize(map);
            initializeGeoMapControls();

            updateChart();
        };

        Map.updateGeoPanel = function () {
            $geoPanel.hide();
            
            if (FC.state.activePage === "geography") {
                $geoPanel.show();
                $("section.geography").append($geoPanel);
                $(".fc-map-icon.search").fadeIn();
                if (FC.mapEntities) {
                    FC.mapEntities.showAll();
                    FC.mapEntities.hideProbe();
                }
                FC.Map.enableNavigation();
                $d3Div.hide();
            }

            if (FC.state.activePage === "results") {
                if (probeMode) {
                    FC.Map.disableNavigation();
                }

                $geoPanel.show();
                $("section.results").append($geoPanel);
                $(".fc-map-icon.search").fadeOut();
                if (FC.mapEntities) FC.mapEntities.hideAll();
                $d3Div.show();
            }

            if (FC.state.activePage === "export") {
                $geoPanel.show();
                $("section.export").append($geoPanel);
                $(".fc-map-icon.search").fadeOut();
                if (FC.mapEntities) FC.mapEntities.hideAll();
                $d3Div.show();
            }
        };

        Map.drawVariableData = function (vname, dataMode) {
            vname = vname || FC.state.selectedVariable;
            dataMode = dataMode || FC.state.dataMode;

            var options = {
                yearSlice: FC.state.yearSlice,
                daySlice: FC.state.daySlice,
                hourSlice: FC.state.hourSlice,
                dataMode: dataMode ? dataMode : "values"
            };

            FC.state.grids.forEach(function (grid) {
                Map.drawGridData(grid, vname, options);
            });

            var points = [];

            FC.state.points.forEach(function (point) {
                points.push(point);
            });

            if (points.length) {
                Map.clearPointMarkers();
                Map.drawPointData(points, vname, options);
            }
        };

        Map.drawGridData = function (grid, vname, options) {
            var heatmap,
                data = {
                    x: grid.getLonGrid(),
                    y: grid.getLatGrid(),
                    f: FC.state.getGridData(grid, options)
                },
                palette;

            if (!data.f) {
                return;
            }

            palette = FC.state.palette;

            if (!grid.heatmap) {
                heatmap = plot.heatmap("heatmap " + heatMapId++, data);
                heatmap.yDataTransform = D3.mercatorTransform;
                heatmap.palette = palette;
                heatmap.isToolTipEnabled = false;
                grid.heatmap = heatmap;
                updateChart();
            } else {
                heatmap = grid.heatmap;
                heatmap.palette = palette;
                heatmap.draw(data);
            }

            updateChart();
        };

        Map.drawPointData = function (points, vname, options) {
            var palette = FC.state.palette,
                pointsData = FC.state.getPointsData(points, options),
                data = {
                    x: pointsData.x,
                    y: pointsData.y,
                    shape: FC.Settings.DEFAULT_MARKER_SHAPE,
                    value: pointsData.f,
                    border: FC.Settings.DEFAULT_MARKER_BORDER,
                    size: FC.Settings.DEFAULT_MARKER_SIZE
                };

                // Convert values to rgba color.
                pointsData.f = pointsData.f.map(function (value) {
                    if (isNaN(value)) {
                        value = new Microsoft.Maps.Color(FC.Settings.DEFAULT_NAN_VALUE_COLOR.a,
                            FC.Settings.DEFAULT_NAN_VALUE_COLOR.r,
                            FC.Settings.DEFAULT_NAN_VALUE_COLOR.g,
                            FC.Settings.DEFAULT_NAN_VALUE_COLOR.b);
                    }
                    else {
                        value = palette.getRgba(value);
                    }

                    return "rgba(" + value.r + "," + value.g + "," + value.b + "," + value.a + ")";
                });

                data.color = pointsData.f;

            // Markers plot draw every point that is in 'completed' state.
            if (!_markersPlot) {
                _markersPlot = plot.markers("markers", data);
                _markersPlot.yDataTransform = D3.mercatorTransform;
                _markersPlot.isToolTipEnabled = false;
                updateChart();
            }
            else {
                _markersPlot.draw(data);
                updateChart();
            }

            // Hacks to prevent D3 bug when plots are in wrong position.
            updateChart();
        };

        Map.clearAllHeatmaps = function () {
            FC.state.grids.forEach(function (grid) {
                if (grid.heatmap) {
                    grid.heatmap.remove();
                    grid.heatmap = undefined;
                }
            });
        };

        Map.clearPointMarkers = function () {
            if (_markersPlot) {
                _markersPlot.remove();
                _markersPlot = undefined;
            }
        };

        Map.updateChartOnMapLoad = function () {
            var watch = function () {
                if ($geoPanel.is(":visible")) {
                    updateChart();
                } else {
                    setTimeout(watch, 50);
                }
            };
            setTimeout(watch, 50);
        };

        /**
         * Enables navigation of Bing Maps.
         */
        Map.enableNavigation = function () {
            map.setOptions({
                disablePanning: false,
                disableZooming: false
            });
        };

        /**
         * Disables navigation of Bing Maps.
         */
        Map.disableNavigation = function () {
            map.setOptions({
                disablePanning: true,
                disableZooming: true
            });
        };

        Map.toggleProbeMode = function () {
            probeMode = !probeMode;

            if (probeMode) {
                Map.disableNavigation();
            }
            else {
                FC.mapEntities.hideProbe();
                Map.enableNavigation();
            }
        };

        /**
         * If given location is inside some grid or marker then returns:
         *  - float number or NaN in 'values' and 'uncertainty' data mode;
         *  - data source name in 'provenance' data mode.
         *
         * If no grid and marker contain given location then return null or undefined.
         */
        Map.getValueByLocation = function (loc) {
            var value,
                hm,
                tooltip;

            FC.state.grids.some(function (grid) {
                hm = grid.heatmap;

                if (hm && (typeof value === "undefined" || value === null)) {
                    value = hm.getValue(loc.longitude, loc.latitude);
                }
            });

            // Show value of marker (first marker if mouse cursor is above two or more markers) if mouse cursor is above any.
            if (_markersPlot) {
                tooltip = _markersPlot.findToolTipMarkers(loc.longitude, loc.latitude)[0];
            }

            if (tooltip) {
                value = tooltip.value;
            }

            // Show data source name as value in provenance data mode.
            if (FC.state.dataMode === "provenance") {
                value = FC.state.getDataSourceById(value).Name;
            }

            return value;
        };
  
        function initializeGeoMapControls() {
            map.setView({
                // Display Europe and surroundings for a start
                bounds: new Microsoft.Maps.LocationRect.fromEdges(58, -2, 20, 45)
            });


            $(".fc-map-icon.zoom-in").click(function (event) {
                event.stopPropagation();
                map.setView({
                    animate: true,
                    zoom: map.getZoom() + 1
                });
            });

            $(".fc-map-icon.zoom-out").click(function (event) {
                event.stopPropagation();
                map.setView({
                    animate: true,
                    zoom: map.getZoom() - 1
                });
            });

            $(".fc-map-icon.search").click(function (event) {
                event.stopPropagation();
                mapSearchPanel.show();
            });

            $(".fc-map-icon.options").click(function (event) {
                event.stopPropagation();
                mapOptionsPanel.show();
            });

            updateChart();
        }

        /**
         * Updates size and position of d3 master plot such that it covers Bing maps visible area.
         */
        var updateChart = function () {
            var bounds = map.getBounds();
            var mapCenter = map.getCenter();
            var mapWidth = map.getWidth();
            var mapHeight = map.getHeight();

            var deltaLon = 30;
            var firstPoint = map.tryLocationToPixel({ latitude: 0, longitude: mapCenter.longitude }, Microsoft.Maps.PixelReference.control);
            var secondPoint = map.tryLocationToPixel({ latitude: 0, longitude: mapCenter.longitude + deltaLon }, Microsoft.Maps.PixelReference.control);
            var pixelDelta = secondPoint.x - firstPoint.x;

            if (pixelDelta < 0)
                pixelDelta = firstPoint.x - map.tryLocationToPixel({ latitude: 0, longitude: mapCenter.longitude - deltaLon }, Microsoft.Maps.PixelReference.control).x;

            var pixelsInDegree = pixelDelta / deltaLon;
            var degs = mapWidth / pixelsInDegree;
            var left = mapCenter.longitude - degs / 2;
            var right = mapCenter.longitude + degs / 2;

            var topPixel = map.tryLocationToPixel({ latitude: bounds.getNorth(), longitude: mapCenter.longitude }, Microsoft.Maps.PixelReference.control);
            var bottomPixel = map.tryLocationToPixel({ latitude: bounds.getSouth(), longitude: mapCenter.longitude }, Microsoft.Maps.PixelReference.control);

            $d3Div.css('left', -mapWidth / 2);
            $d3Div.css('top', -mapHeight / 2 + topPixel.y);
            $d3Div.width(mapWidth);
            $d3Div.height(bottomPixel.y - topPixel.y);

            var top = bounds.getNorth();
            var bottom = bounds.getSouth();

            plot.navigation.setVisibleRect({ 
                x: left, 
                y: D3.mercatorTransform.dataToPlot(bottom),
                width: degs, 
                height: D3.mercatorTransform.dataToPlot(top) - D3.mercatorTransform.dataToPlot(bottom)
                }, false);

            plot.updateLayout();
        };

        /**
         * Hides or show probe for given point of map.
         * Hides if value in given point is NaN or undefined, updates probe data with value and location of given point on map and shows probe othwerwise.
         */
        var updateProbe = function (event) {
            var hm, ct, dt, v;
            var px, py, dx, dy;
            var description, tooltip;
            var loc = FC.mapEntities.map.tryPixelToLocation({ x: event.getX(), y: event.getY() });
            var pos = FC.mapEntities.map.tryLocationToPixel(loc, Microsoft.Maps.PixelReference.control);
            var sx = pos.x;
            var sy = pos.y;

            FC.state.grids.some(function (grid) {
                hm = grid.heatmap;
                if (hm && (typeof v === "undefined" || v === null)) {
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

            if (_markersPlot) {
                if (!ct || !dt) {
                    ct = _markersPlot.coordinateTransform;
                    dt = _markersPlot.yDataTransform;
                    px = ct.screenToPlotX(sx);
                    py = ct.screenToPlotY(sy);
                    dx = px;
                    dy = dt.plotToData(py);
                }

                tooltip = _markersPlot.findToolTipMarkers(dx, dy, px, py)[0];
            }

            if (tooltip) {
                dx = tooltip.x;
                dy = tooltip.y;
                v = tooltip.value;
            }

            // Show data source name is value in 'provenance' data mode.
            if (typeof v !== "undefined" && v !== null) {
                if (FC.state.dataMode === "provenance") {
                    v = FC.state.getDataSourceById(v).Name;
                }
            }

            if (typeof v !== "undefined" && v !== null) {
                description = FC.state.config.EnvironmentalVariables.filter(function (variable) {
                    return variable.Name === FC.state.selectedVariable;
                })[0].Description;

                FC.mapEntities.updateProbe({
                    vname: description,
                    value: v,
                    longitude: dx,
                    latitude: dy
                });
            }
            else {
                FC.mapEntities.hideProbe();
            }
        };

        return Map;
    })(FC.Map || (FC.Map = {}), window, $);

    return FC;
})(FC || {}, window, jQuery);