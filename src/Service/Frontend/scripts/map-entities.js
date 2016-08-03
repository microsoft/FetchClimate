var FC = (function (FC, window, $) {
    var MM = Microsoft.Maps;
    var mouseEvents = "click dblclick hover mousedown mouseenter mouseleave mousemove mouseout mouseover mouseup toggle";

    FC.MapEntities = function (map, options) {
        var self = this;

        // Constants.
        // -------------------------

        var MIN_LAT = -90, // degrees
            MAX_LAT = 90, // degrees
            MIN_LON = -180, // degrees
            MAX_LON = 180, // degrees
            PERIOD_LON = 360, // degrees
            HALF_PERIOD_LON = 180, // degrees
            BOUNDARY_DELTA = 0.01, // degrees
            MAX_LABEL_HEIGHT = 26; // px

        // Private variables.
        // -------------------------

        var _map = map,
            _$root = $(_map.getRootElement()),
            _layer = _map.entities,
            _points = {},
            _regions = {},
            _isDragging = false,
            _isActive = false,
            _blockMapEventsOnce = false,
            _boundary = {},
            _lonPerPixel,
            _mode,

            _startMousePos,
            _prevMousePos,
            _curMousePos,
            _startMouseLoc,
            _prevMouseLoc,
            _curMouseLoc,

            _curPoint,
            _curPushpin,
            _curRegion,
            _curRectangles,
            _curBorders,
            _curDragHandles,
            _curLabel,

            _onClickId,
            _onMouseDownId,
            _onMouseMoveId,
            _onMouseUpId,

            _$leftOverlay,
            _$rightOverlay,
            _leftPeriodPolyline,
            _rightPeriodPolyline,
            _centerPeriodPolyline,

            _mapProbe,

            _onModeComplete = function () {};

        // Default options.
        // -------------------------

        var _options = {
            pointPushpinOptions: {
                icon: "images/point-pushpin.png",
                width: 43,
                height: 56,
                zIndex: 2
            },
            pointActivePushpinOptions: {
                icon: "images/point-pushpin-active.png",
                width: 43,
                height: 56,
                zIndex: 2,
                draggable: true
            },
            pointLabelOptions: {
                zIndex: 3
            },
            pointActiveLabelOptions: {
                zIndex: 3,
                anchor: new MM.Point(-25, 45)
            },
            regionRectOptions: {
                fillColor: new MM.Color(64, 0, 172, 255),
                strokeColor: new MM.Color(255, 0, 172, 255),
                strokeThickness: 0
            },
            regionActiveRectOptions: {
                fillColor: new MM.Color(64, 216, 30, 0),
                strokeColor: new MM.Color(255, 216, 30, 0),
                strokeThickness: 0
            },
            regionBorderOptions: {
                strokeColor: new MM.Color(255, 0, 172, 255)
            },
            regionActiveBorderOptions: {
                strokeColor: new MM.Color(255, 216, 30, 0),
            },
            dragHandleOptions: {
                icon: "images/draghandle.png",
                width: 20,
                height: 20,
                anchor: new MM.Point(10, 10),
                zIndex: 1,
                draggable: true
            },
            regionLabelOptions: {
                zIndex: 3,
                anchor: new MM.Point(-10, 0)
            },
            regionActiveLabelOptions: {
                zIndex: 3,
                anchor: new MM.Point(-10, 0)
            },
            periodPolylineOptions: {
                strokeColor: new MM.Color(64, 216, 30, 0),
                strokeDashArray: "8 8",
                strokeThickness: 3
            },
            mapProbeInfoboxOptions: {
                width: 140,
                height: 126,
                zIndex: 10000,
                visible: false
            }
        };

        // Public Properties.
        // -------------------------

        Object.defineProperties(self, {
            map: { get: function () { return _map; } },
            layer: { get: function () { return _layer; } },
            mode: { get: function () { return _mode; } },
            isDragging: { get: function () { return _isDragging; } },
            isActive: { get: function () { return _isActive; } },
            boundary: { get: function () { return _boundary; } },
            lonPerPixel: { get: function () { return _lonPerPixel; } },
            points: { get: function () { return _points; } },
            regions: { get: function () { return _regions; } },
            probe: { get: function () { return _mapProbe; } }
        });

        // Private Functions.
        // -------------------------

        function initialize() {
            // Load user defined options.
            loadOptions(options);

            // Track mouse position and location.
            MM.Events.addHandler(_map, "mousemove", trackMouse);
            MM.Events.addHandler(_map, "mousedown", trackMouse);

            // Add overlays to non-main periods.
            addPeriodOverlays();

            // Add period polylines.
            addPeriodPolylines();

            // Update period polylines, overlays and boundary value.
            MM.Events.addHandler(_map, "viewchange", onMapViewChange);
            watchMapLoad(onMapViewChange);

            // Initialize map probe infobox.
            _mapProbe = new MM.Infobox(new MM.Location(), _options.mapProbeInfoboxOptions);
            _map.entities.push(_mapProbe);
        }

        /**
         * Loads user defined options to private variable.
         * @param  {Object} options User defined options. See Default options for more info.
         */
        function loadOptions(options) {
            for (var optionName in options) {
                _options[optionName] = options[optionName];
            }
        }

        /**
         * Watches for full map load (the position of the map is set) and fires handler.
         */
        function watchMapLoad(handler) {
            var watch = function (handler) {
                if (_map.getPageX() !== 0) {
                    handler();
                } else {
                    setTimeout(watch, 50, handler);
                }
            };
            setTimeout(watch, 50, handler);
        }

        /**
         * Adds overlays to the DOM for non-main periods of the map.
         */
        function addPeriodOverlays() {
            var $overlays;

            _$leftOverlay = $("<div></div>").addClass("period-overlay");
            _$rightOverlay = $("<div></div>").addClass("period-overlay");
            $overlays = _$leftOverlay.add(_$rightOverlay);
            $overlays.on("click mousedown", function (event) {
                event.stopPropagation();
            });
            _$root.append($overlays);
        }

        /**
         * Adds vertical lines for the main period's boundaries and 180 meridian.
         */
        function addPeriodPolylines() {
            var clon = _map.getCenter().longitude,
                leftPoints = [
                    new MM.Location(MAX_LAT, clon + MIN_LON),
                    new MM.Location(MIN_LAT, clon + MIN_LON)
                ],
                centerPoints = [
                    new MM.Location(MAX_LAT, MAX_LON),
                    new MM.Location(MIN_LAT, MAX_LON)
                ],
                rightPoints = [
                    new MM.Location(MAX_LAT, clon + MAX_LON),
                    new MM.Location(MIN_LAT, clon + MAX_LON)
                ],
                opts = _options.periodPolylineOptions;

            _leftPeriodPolyline = new MM.Polyline(leftPoints, opts);
            _centerPeriodPolyline = new MM.Polyline(centerPoints, opts);
            _rightPeriodPolyline = new MM.Polyline(rightPoints, opts);
            _layer.push(_centerPeriodPolyline);

            // NOTE: DEBUG ONLY.
            // _layer.push(_leftPeriodPolyline);
            // _layer.push(_rightPeriodPolyline);
        }

        /**
         * Updates overlays for non-main periods of the map.
         * @param  {Number} llon A longitude of the left main period's boundary.
         * @param  {Number} rlon A longitude of the right main period's boundary.
         */
        function updatePeriodOverlays(llon, rlon) {
            var lb = new MM.Location(0, llon),
                rb = new MM.Location(0, rlon),
                lx = _map.tryLocationToPixel(lb, MM.PixelReference.page).x,
                rx = _map.tryLocationToPixel(rb, MM.PixelReference.page).x,
                mx = _map.getPageX(),
                my = _map.getPageY(),
                mw = _map.getWidth(),
                mh = _map.getHeight();

            _$leftOverlay
                .width(lx - mx)
                .height(mh)
                .css({
                    left: mx,
                    top: my
                });

            _$rightOverlay
                .width(mx + mw - rx)
                .height(mh)
                .css({
                    left: rx,
                    top: my
                });
        }

        /**
         * NOTE: DEBUG ONLY.
         * Updates vertical lines for the main period's boundaries.
         * @param  {Number} llon A longitude of the left main period's boundary.
         * @param  {Number} rlon A longitude of the right main period's boundary.
         */
        function updatePeriodPolylines(llon, rlon) {
            var llocs = _leftPeriodPolyline.getLocations(),
                clocs = _centerPeriodPolyline.getLocations(),
                rlocs = _rightPeriodPolyline.getLocations();

            llocs.forEach(function (loc) {
                loc.longitude = llon;
            });

            rlocs.forEach(function (loc) {
                loc.longitude = rlon;
            });

            _leftPeriodPolyline.setLocations(llocs);
            _rightPeriodPolyline.setLocations(rlocs);
        }

        /**
         * Sets cursor type for the map.
         * @param {String} cursor A valid value for 'cursor' CSS rule.
         */
        function setMouseCursor(cursor) {
            _$root.attr("style", function (i, s) {
                return s + "cursor: " + cursor + " !important;";
            });
        }

        /**
         * This handler is fired on map's 'viewchange' event.
         * Updates boundary's information, overlays and auxilary vertical lines.
         * @param  {Object} event Bing Maps Event object.
         */
        function onMapViewChange(event) {
            var clon = _map.getCenter().longitude,
                llon = clon + MIN_LON + BOUNDARY_DELTA,
                rlon = clon + MAX_LON - BOUNDARY_DELTA;

            _boundary.leftLon = llon;
            _boundary.centerLon = MAX_LON;
            _boundary.rightLon = rlon;
            _boundary.leftX = _map.tryLocationToPixel(new MM.Location(0, llon)).x;
            _boundary.centerX = _map.tryLocationToPixel(new MM.Location(0, MAX_LON)).x;
            _boundary.rightX = _map.tryLocationToPixel(new MM.Location(0, rlon)).x;
            _lonPerPixel = PERIOD_LON / Math.abs(_boundary.rightX - _boundary.leftX);

            updatePeriodOverlays(llon, rlon);

            // NOTE: DEBUG ONLY.
            // updatePeriodPolylines(llon, rlon);
        }

        /**
         * This handler is fired on map's 'click' event.
         * Works in Point mode only.
         * Adds a new point to the map.
         * @param  {Object} event Bing Maps Event object.
         */
        function onMapClick(event) {
            if (FC.Controls.Panel.isAnyRightPanelVisible()) return;

            if (_blockMapEventsOnce) { _blockMapEventsOnce = false; return; }
            self.addPoint(_curMouseLoc.latitude, _curMouseLoc.longitude, { isActive: true });
            disableMode(_curPoint, true);
        }

        /**
         * This handler is fired on map's 'mousedown' event.
         * Works in Region mode only.
         * Starts drag'n'drop region adding process.
         * @param  {Object} event Bing Maps Event object.
         */
        function onMapMouseDown(event) {
            if (FC.Controls.Panel.isAnyRightPanelVisible()) return;

            _isDragging = true;
            _startMousePos = { x: event.getX(), y: event.getY() };
            _startMouseLoc = _map.tryPixelToLocation(_startMousePos);
        }

        /**
         * This handler is fired on map's 'mousemove' event.
         * Works in Region mode only.
         * Handles drag'n'drop region adding process. Rerenders
         * the region if all conditions are met.
         * @param  {Object} event Bing Maps Event object.
         */
        function onMapMouseMove(event) {
            var minMaxLocs;

            if (_isDragging && !isIntersecting180()) {
                minMaxLocs = getMinMaxLocations(_startMouseLoc, _curMouseLoc);
                addRectangles(
                    minMaxLocs.min.latitude,
                    minMaxLocs.min.longitude,
                    minMaxLocs.max.latitude,
                    minMaxLocs.max.longitude,
                    true
                );
            }
        }

        /**
         * This handler is fired on map's 'mouseup' event.
         * Works in Region mode only.
         * Ends drag'n'drop region adding process.
         * @param  {Object} event Bing Maps Event object.
         */
        function onMapMouseUp(event) {
            _isDragging = false;

            if (!_curRegion || !_curRegion.min || !_curRegion.max) {
                disableMode(null, true);
                return;
            }

            var minlat = _curRegion.min.latitude,
                minlon = _curRegion.min.longitude,
                maxlat = _curRegion.max.latitude,
                maxlon = _curRegion.max.longitude;

            // Remove temp region.
            _layer.remove(_curRegion);

            if (maxlat <= minlat || maxlon <= minlon) {
                disableMode(null, true);
                return;
            }

            self.addRegion(minlat, minlon, maxlat, maxlon, { isActive: true });
            disableMode(_curRegion, true);
        }

        /**
         * Checks intersection of the region between two
         * points with 180 meridian.
         * @param  {Number}  fixedX   Viewport X coordinate of a fixed point.
         * @param  {Number}  movableX Viewport X coordinate of a movable point.
         * @return {Boolean}          True if there is an intersection, false otherwise.
         */
        function isIntersecting180(fixedX, movableX) {
            var lx = _boundary.leftX,
                cx = _boundary.centerX,
                rx = _boundary.rightX,
                fx = fixedX || _startMousePos.x,
                mx = movableX || _curMousePos.x,
                w = Math.abs(mx - fx),
                lw = Math.abs(cx - lx),
                rw = Math.abs(rx - cx),
                gw = Math.abs(cx - fx),
                pw = lw + rw;

            return (fx <= cx && cx <= mx) ||
                   (mx <= cx && cx <= fx) ||
                   (pw - w <= gw && fx <= rx && rx <= mx) ||
                   (pw - w <= gw && mx <= lx && lx <= fx);
        }

        /**
         * Checks intersection of the region between two
         * points with the main period's boundaries.
         * @param  {Number}  minlon Minimum longitude of the region.
         * @param  {Number}  maxlon Maximum longitude of the region.
         * @return {Boolean}        True if there is an intersection, false otherwise.
         */
        function isIntersectingBoundary(minlon, maxlon) {
            var lblon = _boundary.leftLon,
                rblon = _boundary.rightLon,
                blon = (MIN_LON <= rblon && rblon <= MAX_LON) ? rblon : lblon;

            return (MIN_LON <= minlon && minlon <= blon) &&
                   (blon <= maxlon && maxlon <= MAX_LON);
        }

        /**
         * Determines minimum and maximum locations by two
         * region's points.
         * @param  {MM.Location} fixedLoc   Location of a fixed point.
         * @param  {MM.Location} movableLoc Location of a movable point.
         * @return {Object}                 Object with 'min' and 'max' MM.Location properties.
         */
        function getMinMaxLocations(fixedLoc, movableLoc) {
            var minLoc, maxLoc,
                slat = fixedLoc.latitude,
                slon = fixedLoc.longitude,
                clat = movableLoc.latitude,
                clon = movableLoc.longitude;

            minLoc = new MM.Location((slat < clat) ? slat : clat, (slon < clon) ? slon : clon);
            maxLoc = new MM.Location((slat > clat) ? slat : clat, (slon > clon) ? slon : clon);

            return {
                min: minLoc,
                max: maxLoc
            };
        }

        /**
         * The main drawing function for Region mode.
         * Adds region's rectangles and its borders by
         * minimum and maximum latitude and longitude.
         * @param {Number}  minlat   Minimum latitude of the region.
         * @param {Number}  minlon   Minimum longitude of the region.
         * @param {Number}  maxlat   Maximum latitude of the region.
         * @param {Number}  maxlon   Maximum longitude of the region.
         * @param {Boolean} isActive Is this region is selected after adding.
         */
        function addRectangles(minlat, minlon, maxlat, maxlon, isActive) {
            var rectOpts = isActive ? _options.regionActiveRectOptions : _options.regionRectOptions,
                borderOpts = isActive ? _options.regionActiveBorderOptions : _options.regionBorderOptions,
                knots = [],
                width = maxlon - minlon,
                isIntersecting = false,
                lblon = _boundary.leftLon,
                rblon = _boundary.rightLon,
                blon = (MIN_LON <= rblon && rblon <= MAX_LON) ? rblon : lblon,
                llon, cllon, clon, crlon, rlon,
                lw, rw;

            /**
             * Draws rectangles and borders by calculated knots.
             */
            var drawRectangles = function () {
                var tl, tcl, tc, tcr, tr,
                    bl, bcl, bc, bcr, br,
                    lp, cp, rp;

                // Get corners of the region.
                tl = new MM.Location(maxlat, minlon);
                tr = new MM.Location(maxlat, maxlon);
                bl = new MM.Location(minlat, minlon);
                br = new MM.Location(minlat, maxlon);

                // Depending on how much divisions of the region
                // draw corresponding number of rectangles and borders.
                switch (knots.length) {
                case 2:
                    lp = [tr, tl, bl, br, tr];
                    _curRectangles.push(new MM.Polygon(lp, rectOpts));
                    _curBorders.push(new MM.Polyline(lp, borderOpts));
                    break;
                case 3:
                    clon = knots[1];
                    tc = new MM.Location(maxlat, clon);
                    bc = new MM.Location(minlat, clon);
                    lp = [tl, tc, bc, bl];
                    rp = [tr, tc, bc, br];
                    _curRectangles.push(new MM.Polygon(lp, rectOpts));
                    _curRectangles.push(new MM.Polygon(rp, rectOpts));
                    _curBorders.push(new MM.Polyline([tc, tl], borderOpts));
                    _curBorders.push(new MM.Polyline([tl, bl], borderOpts));
                    _curBorders.push(new MM.Polyline([bc, bl], borderOpts));
                    _curBorders.push(new MM.Polyline([tc, tr], borderOpts));
                    _curBorders.push(new MM.Polyline([tr, br], borderOpts));
                    _curBorders.push(new MM.Polyline([bc, br], borderOpts));
                    break;
                case 4:
                    cllon = knots[1];
                    crlon = knots[2];
                    tcl = new MM.Location(maxlat, cllon);
                    tcr = new MM.Location(maxlat, crlon);
                    bcl = new MM.Location(minlat, cllon);
                    bcr = new MM.Location(minlat, crlon);
                    lp = [tl, tcl, bcl, bl];
                    rp = [tr, tcr, bcr, br];
                    _curRectangles.push(new MM.Polygon(lp, rectOpts));
                    _curRectangles.push(new MM.Polygon(rp, rectOpts));
                    _curBorders.push(new MM.Polyline([tcl, tl], borderOpts));
                    _curBorders.push(new MM.Polyline([tl, bl], borderOpts));
                    _curBorders.push(new MM.Polyline([bcl, bl], borderOpts));
                    _curBorders.push(new MM.Polyline([tcr, tr], borderOpts));
                    _curBorders.push(new MM.Polyline([tr, br], borderOpts));
                    _curBorders.push(new MM.Polyline([bcr, br], borderOpts));

                    // Draw center rectangle with borders for left part
                    // and right part in different order.
                    if (lw > HALF_PERIOD_LON) {
                        cp = [tcl, tcr, bcr, bcl];
                        _curRectangles.push(new MM.Polygon(cp, rectOpts));
                        _curBorders.push(new MM.Polyline([tcr, tcl], borderOpts));
                        _curBorders.push(new MM.Polyline([bcr, bcl], borderOpts));
                    } else if (rw > HALF_PERIOD_LON) {
                        cp = [tcr, tcl, bcl, bcr];
                        _curRectangles.push(new MM.Polygon(cp, rectOpts));
                        _curBorders.push(new MM.Polyline([tcl, tcr], borderOpts));
                        _curBorders.push(new MM.Polyline([bcl, bcr], borderOpts));
                    }

                    break;
                }

                // Save corners for future use.
                _curRegion.locations = [tl, bl, br, tr];
            };

            // Ignore invalid rectangles.
            if (width > PERIOD_LON) return;

            // Update boundaries.
            onMapViewChange();

            // Intersection criteria.
            isIntersecting = isIntersectingBoundary(minlon, maxlon);

            // Store properties for future use.
            _curRegion.min = { latitude: minlat, longitude: minlon };
            _curRegion.max = { latitude: maxlat, longitude: maxlon };
            _curRegion.isIntersecting = isIntersecting;

            llon = minlon;
            rlon = maxlon;

            // If intersecting, then divide region on two parts
            // by the boundary of the main period.
            if (isIntersecting) {
                clon = blon;
                lw = clon - llon;
                rw = rlon - clon;

                // If one of the parts are greater than 180 degrees,
                // then divide this part on two equal parts.
                if (lw > HALF_PERIOD_LON) {
                    cllon = (llon + blon) / 2;
                    crlon = clon;
                    knots = [llon, cllon, crlon, rlon];
                } else if (rw > HALF_PERIOD_LON) {
                    cllon = clon;
                    crlon = (rlon + blon) / 2;
                    knots = [llon, cllon, crlon, rlon];
                } else {
                    knots = [llon, clon, rlon];
                }
            } else if (width > HALF_PERIOD_LON) {
                clon = (llon + rlon) / 2;
                knots = [llon, clon, rlon];
            } else {
                knots = [llon, rlon];
            }

            // Draw the region.
            _curRectangles.clear();
            _curBorders.clear();
            drawRectangles();
        }

        /**
         * Draws drag handles for the region.
         */
        function addDragHandles() {
            var i, dh,
                locs = _curRegion.locations,
                isActive = _curRegion.isActive,
                opts = _options.dragHandleOptions;

            for (i = 0; i < 4; ++i) {
                dh = new MM.Pushpin(locs[i], opts);
                _curDragHandles.push(dh);
            }

            if (!isActive) {
                _curDragHandles.setOptions({ visible: false });
            }
        }

        /**
         * Add event handlers for new created point on the map.
         * Events: out of point click, point click, point drag'n'drop.
         */
        function addPointHandlers(point) {
            var pushpin = point.get(0),
                label = point.get(1),
                eventHandlerIds = [];

            var onOutPointClick = function (event) {
                if (pushpin !== event.target && label !== event.target) {
                    point.isActive = false;
                    pushpin.setOptions(_options.pointPushpinOptions);
                    setPointLabel(point);

                    FC.Geography.unselectPoint(point.id);
                }
            };

            var onPointClick = function (event) {
                point.isActive = true;
                pushpin.setOptions(_options.pointActivePushpinOptions);
                setPointLabel(point);

                FC.Geography.selectPoint(point.id);
            };

            var onPointDragStart = function (event) {
                FC.Map.disableNavigation();

                if (_isActive) {
                    removeModeEventHandlers();
                }
            };

            var onPointDrag = function (event) {
                label.setLocation(event.entity.getLocation());
                FC.Geography.updatePoint(point.id, {
                    longitude: event.entity.getLocation().longitude,
                    latitude: event.entity.getLocation().latitude
                });
            };

            var onPointDragEnd = function (event) {
                FC.Geography.updatePoint(point.id, {
                    longitude: event.entity.getLocation().longitude,
                    latitude: event.entity.getLocation().latitude
                }, true);

                FC.Map.enableNavigation();

                if (_isActive) {
                    _blockMapEventsOnce = true;
                    _isDragging = false;
                    proceedCurrentMode();
                }
            };

            eventHandlerIds.push(MM.Events.addHandler(_map, "mousedown", onOutPointClick));
            eventHandlerIds.push(MM.Events.addHandler(pushpin, "mousedown", onPointClick));
            eventHandlerIds.push(MM.Events.addHandler(label, "mousedown", onPointClick));
            eventHandlerIds.push(MM.Events.addHandler(pushpin, "dragstart", onPointDragStart));
            eventHandlerIds.push(MM.Events.addHandler(pushpin, "drag", onPointDrag));
            eventHandlerIds.push(MM.Events.addHandler(pushpin, "dragend", onPointDragEnd));

            point.setId = function (id) {
                point.id = id;
                _points[id] = point;
            };

            point.triggerPointClick = function () {
                onPointClick();
            };

            point.triggerOutPointClick = function () {
                onOutPointClick({ target: _map });
            };

            point.destroy = function () {
                eventHandlerIds.forEach(function (id) {
                    MM.Events.removeHandler(id);
                    point = null;
                });
            };
        }

        /**
         * Add event handlers for new created region on the map.
         * Events: out of region click, region click, region's drag handles drag'n'drop.
         */
        function addRegionHandlers(region) {
            var i, dh,
                fixedDragHandle,
                curDragHandle,
                curIdx,
                dragHandles = region.get(0),
                rectangles = region.get(1),
                borders = region.get(2),
                label = region.get(3),
                eventHandlerIds = [];

            var onOutRegionClick = function (event) {
                var isOutOfRegionClick = rectangles.indexOf(event.target) === -1 &&
                                         dragHandles.indexOf(event.target) === -1 &&
                                         borders.indexOf(event.target) === -1 &&
                                         label !== event.target;

                if (isOutOfRegionClick) {
                    region.isActive = false;
                    for (i = 0; i < rectangles.getLength(); ++i) {
                        rectangles.get(i).setOptions(_options.regionRectOptions);
                    }
                    for (i = 0; i < borders.getLength(); ++i) {
                        borders.get(i).setOptions(_options.regionBorderOptions);
                    }
                    dragHandles.setOptions({ visible: false });
                    setRegionLabel(region);

                   FC.Geography.unselectGrid(region.id);
                }
            };

            var onRegionClick = function (event) {
                if (!_isActive || !event) {
                    region.isActive = true;
                    for (i = 0; i < rectangles.getLength(); ++i) {
                        rectangles.get(i).setOptions(_options.regionActiveRectOptions);
                    }
                    for (i = 0; i < borders.getLength(); ++i) {
                        borders.get(i).setOptions(_options.regionActiveBorderOptions);
                    }
                    dragHandles.setOptions({ visible: true });
                    setRegionLabel(region);
                   
                    FC.Geography.selectGrid(region.id);
                } else {
                    onOutRegionClick({ target: _map });
                }
            };

            var onHandleDragStart = function (event) {
                FC.Map.disableNavigation();
                curDragHandle = event.entity;
                curIdx = dragHandles.indexOf(curDragHandle);
                fixedDragHandle = dragHandles.get((curIdx + 2) % 4);

                FC.Geography.selectGrid(region.id);

                if (_isActive) {
                    removeModeEventHandlers();
                }
            };

            var onHandleDrag = function (event) {
                var i, dh, loc,
                    curDragHandle = event.entity,
                    fixedHandleLoc = fixedDragHandle.getLocation(),
                    curHandleLoc = curDragHandle.getLocation(),
                    minMaxLocs = getMinMaxLocations(fixedHandleLoc, curHandleLoc),
                    ib = region.isIntersecting,
                    fx = _map.tryLocationToPixel(fixedHandleLoc).x,
                    mx = _map.tryLocationToPixel(curHandleLoc).x,
                    pw = Math.abs(_boundary.rightX - _boundary.leftX);

                if (minMaxLocs.max.latitude <= minMaxLocs.min.latitude ||
                    minMaxLocs.max.longitude <= minMaxLocs.min.longitude) return;

                // mx is always inside the main period because of tryLocationToPixel transform.
                // Shift mx out of the main period to imitate dragging out of the main period
                // if the region intersects boundary of the main period.
                // TODO: For now it doesn't work for complex cases when mouse cursor is outside
                //       of the main period or if there is any intersection with boundaries.
                if (!isIntersecting180(fx, ib ? (mx < fx ? mx + pw : mx - pw) : mx)) {
                    if (curIdx % 2) {
                        dh = dragHandles.get((curIdx + 5) % 4);
                        loc = dh.getLocation();
                        dh.setLocation(new MM.Location(curHandleLoc.latitude, loc.longitude));
                        dh = dragHandles.get((curIdx + 3) % 4);
                        loc = dh.getLocation();
                        dh.setLocation(new MM.Location(loc.latitude, curHandleLoc.longitude));
                    } else {
                        dh = dragHandles.get((curIdx + 5) % 4);
                        loc = dh.getLocation();
                        dh.setLocation(new MM.Location(loc.latitude, curHandleLoc.longitude));
                        dh = dragHandles.get((curIdx + 3) % 4);
                        loc = dh.getLocation();
                        dh.setLocation(new MM.Location(curHandleLoc.latitude, loc.longitude));
                    }

                    setCurrentRegion(region);
                    addRectangles(
                        minMaxLocs.min.latitude,
                        minMaxLocs.min.longitude,
                        minMaxLocs.max.latitude,
                        minMaxLocs.max.longitude,
                        true
                    );

                    // Update label.
                    label.setLocation(new MM.Location(minMaxLocs.max.latitude, minMaxLocs.min.longitude));
                    setRegionLabel(region);

                    FC.Geography.updateGrid(region.id, {
                        min: {
                            latitude: region.min.latitude,
                            longitude: region.min.longitude
                        },
                        max: {
                            latitude: region.max.latitude,
                            longitude: region.max.longitude
                        }
                    });
                    
                    rectangles = _curRectangles;
                    borders = _curBorders;
                    bindEntityCollection(rectangles, "mousedown", onRegionClick);
                    bindEntityCollection(borders, "mousedown", onRegionClick);
                } else {
                    curDragHandle.setLocation(region.locations[curIdx]);
                }
            };

            var onHandleDragEnd = function (event) {
                FC.Geography.updateGrid(region.id, {
                    min: {
                        latitude: region.min.latitude,
                        longitude: region.min.longitude
                    },
                    max: {
                        latitude: region.max.latitude,
                        longitude: region.max.longitude
                    }
                }, true);

                FC.Map.enableNavigation();

                if (_isActive) {
                    _blockMapEventsOnce = true;
                    _isDragging = false;
                    proceedCurrentMode();
                }
            };

            var updateRegion = function (event) {
                var locs = region.locations,
                    isActive = region.isActive,
                    minLoc = locs[1],
                    maxLoc = locs[3];

                setCurrentRegion(region);
                addRectangles(
                    minLoc.latitude,
                    minLoc.longitude,
                    maxLoc.latitude,
                    maxLoc.longitude,
                    isActive
                );

                // Update label.
                setRegionLabel(region);
                
                rectangles = _curRectangles;
                borders = _curBorders;
                bindEntityCollection(rectangles, "mousedown", onRegionClick);
                bindEntityCollection(borders, "mousedown", onRegionClick);
            };

            eventHandlerIds.push(MM.Events.addHandler(_map, "viewchange", updateRegion));
            eventHandlerIds.push(MM.Events.addHandler(_map, "mousedown", onOutRegionClick));
            eventHandlerIds.concat(bindEntityCollection(rectangles, "mousedown", onRegionClick));
            eventHandlerIds.concat(bindEntityCollection(borders, "mousedown", onRegionClick));
            eventHandlerIds.push(MM.Events.addHandler(label, "mousedown", onRegionClick));

            for (i = 0; i < 4; ++i) {
                dh = dragHandles.get(i);
                MM.Events.addHandler(dh, "dragstart", onHandleDragStart);
                MM.Events.addHandler(dh, "drag", onHandleDrag);
                MM.Events.addHandler(dh, "dragend", onHandleDragEnd);
            }

            region.setId = function (id) {
                region.id = id;
                _regions[id] = region;
            };

            region.triggerRegionClick = function () {
                onRegionClick();
            };

            region.triggerOutRegionClick = function () {
                onOutRegionClick({ target: _map });
            };

            region.destroy = function () {
                eventHandlerIds.forEach(function (id) {
                    MM.Events.removeHandler(id);
                    region = null;
                });
            };
        }

        /**
         * Resets Point/Region creation mode.
         */
        function reset() {
            _mode = null;
            _isDragging = false;
            _isActive = false;
            _onModeComplete = function () {};
            removeModeEventHandlers();
            setMouseCursor("default");
            FC.Map.enableNavigation();
        }

        /**
         * Enables Point creation mode.
         * Binds corresponding map event handlers.
         */
        function enablePointMode() {
            _mode = "point";
            _isActive = true;
            _onClickId = MM.Events.addHandler(_map, "click", onMapClick);
            setMouseCursor("crosshair");
        }

        /**
         * Enables Region creation mode.
         * Binds corresponding map event handlers and
         * prepares variables for a new region.
         */
        function enableRegionMode() {
            _mode = "region";
            _isActive = true;
            _onMouseDownId = MM.Events.addHandler(_map, "mousedown", onMapMouseDown);
            _onMouseMoveId = MM.Events.addHandler(_map, "mousemove", onMapMouseMove);
            _onMouseUpId = MM.Events.addHandler(_map, "mouseup", onMapMouseUp);
            setMouseCursor("crosshair");

            // Temp region during dragging process.
            _curRegion = new MM.EntityCollection();
            _curDragHandles = new MM.EntityCollection();
            _curRectangles = new MM.EntityCollection();
            _curBorders = new MM.EntityCollection();
            _curRegion.push(_curDragHandles);
            _curRegion.push(_curRectangles);
            _curRegion.push(_curBorders);
            _layer.push(_curRegion);
        }

        /**
         * Disables current mode and resets all variables.
         */
        function disableMode(entity, noReset) {
            _isActive = false;
            if (entity !== null) _onModeComplete(entity);

            if (noReset) {
                proceedCurrentMode();
            } else {
                reset();
            }
        }

        /**
         * Removes creation mode's event handlers.
         */
        function removeModeEventHandlers() {
            MM.Events.removeHandler(_onClickId);
            MM.Events.removeHandler(_onMouseUpId);
            MM.Events.removeHandler(_onMouseMoveId);
            MM.Events.removeHandler(_onMouseDownId);
        }

        /**
         * Proceeds current creation mode after entity creation completed.
         */
        function proceedCurrentMode() {
            removeModeEventHandlers();
            FC.Map.disableNavigation();

            if (_mode === "point") {
                enablePointMode();
            } else if (_mode === "region") {
                enableRegionMode();
            }
        }

        /**
         * Sets current point to a private variable.
         * @param {MM.EntityCollection} point A point to set.
         */
        function setCurrentPoint(point) {
            var pushpin = point.get(0),
                label = point.get(1);

            _curPoint = point;
            _curPushpin = pushpin;
            _curLabel = label;
        }

        /**
         * Sets current region to a private variable.
         * @param {MM.EntityCollection} region A region to set.
         */
        function setCurrentRegion(region) {
            var dragHandles = region.get(0),
                rectangles = region.get(1),
                borders = region.get(2),
                label = region.get(3);

            _curRegion = region;
            _curRectangles = rectangles;
            _curBorders = borders;
            _curDragHandles = dragHandles;
            _curLabel = label;
        }

        /**
         * Sets a label for a given point.
         * @param {MM.EntityCollection} point A point with label.
         */
        function setPointLabel(point) {
            var label = point.get(1),
                $htmlContent = $("<div></div>"),
                options,
                extOptions;

            if (point.isActive) {
                $htmlContent.text(point.name).addClass("point-label active");
                extOptions = { htmlContent: $htmlContent[0].outerHTML };
                options = $.extend(extOptions, _options.pointActiveLabelOptions);
            } else {
                $htmlContent.text("").addClass("point-label");
                extOptions = { htmlContent: $htmlContent[0].outerHTML };
                options = $.extend(extOptions, _options.pointLabelOptions);
            }

            label.setOptions(options);
        }

        /**
         * Sets a label for a given region.
         * @param {MM.EntityCollection} region A region with label.
         */
        function setRegionLabel(region) {
            var label = region.get(3),
                rectangles = region.get(1),
                leftRectLocs = rectangles.get(0).getLocations(),
                minMaxLocs = getMinMaxLocations(leftRectLocs[0], leftRectLocs[2]),
                min = _map.tryLocationToPixel(minMaxLocs.min),
                max = _map.tryLocationToPixel(minMaxLocs.max),
                labelWidth = Math.abs(max.x - min.x),
                rectHeight = Math.abs(max.y - min.y),
                $htmlContent = $("<div></div>"),
                options,
                extOptions;

            if (region.isActive) {
                labelWidth += _options.regionActiveLabelOptions.anchor.x;
                $htmlContent.text(rectHeight > MAX_LABEL_HEIGHT ? region.name : "")
                    .addClass("region-label active")
                    .width(labelWidth);
                extOptions = { htmlContent: $htmlContent[0].outerHTML };
                options = $.extend(extOptions, _options.regionActiveLabelOptions);
            } else {
                labelWidth += _options.regionLabelOptions.anchor.x;
                $htmlContent.text(rectHeight > MAX_LABEL_HEIGHT ? region.name : "")
                    .addClass("region-label")
                    .width(labelWidth);
                extOptions = { htmlContent: $htmlContent[0].outerHTML };
                options = $.extend(extOptions, _options.regionLabelOptions);
            }

            label.setOptions(options);
        }

        /**
         * This handler is fired on map's 'mousemove' event.
         * Tracks mouse position and location and updates them.
         * @param {Object} event Bing Maps Event object.
         */
        function trackMouse(event) {
            _prevMousePos = _curMousePos;
            _prevMouseLoc = _curMouseLoc;
            _curMousePos = { x: event.getX(), y: event.getY() };
            _curMouseLoc = _map.tryPixelToLocation(_curMousePos);
        }

        /**
         * Applies MM.Events.addHandler to MM.EntityCollection.
         * @param  {MM.EntityCollection} collection A collection to which to add a new handler.
         * @param  {String}              event      Bing Maps event name.
         * @param  {Function}            handler    Event handler to bind.
         */
        function bindEntityCollection(collection, event, handler) {
            var i, elem,
                len = collection.getLength(),
                eventHandlerIds = [];

            for (i = 0; i < len; ++i) {
                elem = collection.get(i);
                eventHandlerIds.push(MM.Events.addHandler(elem, event, handler));
            }

            return eventHandlerIds;
        }

        /**
         * Returns html string for probe filled with given data.
         * @params  {Object} info An object with fields for variable description, value in the point, latitude and longitude of the point.
         */
        function probeInfoToHtml(info) {
            var vname = info.vname,
                value = info.value,
                lon = info.longitude,
                lat = info.latitude,
                location;

            if (!info.vname) {
                vname = FC.state.config.EnvironmentalVariables.filter(function (v) {
                    return v.Name === FC.state.selectedVariable;
                })[0].Description;
            }

            if (!info.value) {
                value = "no data";
            }

            if (FC.state.dataMode !== "provenance") {
                if (value !== "no data") {
                    value = FC.roundTo(value, FC.Settings.DISPLAY_PRECISION);
                    value += " " + FC.state.getSelectedEnvironmentalVariable().Units;
                }
            }

            if (!info.lon || !info.lat) {
                location = _mapProbe.getLocation();

                lon = location.longitude;
                lat = location.latitude;
            }

            lon = FC.roundTo(lon, FC.Settings.DISPLAY_PRECISION);
            lat = FC.roundTo(lat, FC.Settings.DISPLAY_PRECISION);

            var html = "<div id='geo-map-probe'>" +
                          "<div class='probe-info'>" +
                            "<div>" +
                              "<span class='info-vname'>" + vname + ":</span>  " +
                              "<span class='info-value'>" + value + "</span>" +
                            "</div>" +
                            "<div>" +
                              "<div class='info-longitude'>Lon.: " + lon + "</div>" +
                              "<div class='info-latitude'>Lat.: " + lat + "</div>" +
                            "</div>" +
                          "</div>" +
                          "<div class='probe-leg'></div>" +
                        "</div>";

            return html;
        }

        // Public Methods.
        // -------------------------

        /**
         * Adds new pushpin point to the map in a given location.
         * @param {Number}  lat      A latitude of the point.
         * @param {Number}  lon      A longitude of the point.
         * @param {String}  params.id       ID of a point.
         * @param {String}  params.name     Name of a point.
         * @param {Boolean} params.isActive Is this point is selected after adding.
         */
        this.addPoint = function (lat, lon, params) {
            var id = params && params.id,
                name = params && params.name ? params.name : self.generatePointName(),
                isActive = params && params.isActive ? params.isActive : false,
                loc = new MM.Location(lat, lon),
                opts = isActive ? _options.pointActivePushpinOptions : _options.pointPushpinOptions;

            _curPoint = new MM.EntityCollection();
            _curPushpin = new MM.Pushpin(loc, opts);
            _curLabel = new MM.Pushpin(loc);

            _curPoint.push(_curPushpin);
            _curPoint.push(_curLabel);

            _curPoint.id = id;
            _curPoint.name = name;
            _curPoint.isActive = isActive;
            _curPoint.latitude = lat;
            _curPoint.longitude = lon;
            setPointLabel(_curPoint);
            addPointHandlers(_curPoint);
            _layer.push(_curPoint);

            if (id) _points[id] = _curPoint;

            return _curPoint;
        };

        /**
         * Adds new region to the map in by given min/max locations.
         * @param {Number}  minlat   Minimum latitude of the region.
         * @param {Number}  minlon   Minimum longitude of the region.
         * @param {Number}  maxlat   Maximum latitude of the region.
         * @param {Number}  maxlon   Maximum longitude of the region.
         * @param {String}  params.id       ID of a region.
         * @param {String}  params.name     Name of a region.
         * @param {Boolean} params.isActive Is this region is selected after adding.
         */
        this.addRegion = function (minlat, minlon, maxlat, maxlon, params) {
            if (maxlat <= minlat || maxlon <= minlon) return;

            var id = params && params.id,
                name = params && params.name ? params.name : self.generateRegionName(),
                isActive = params && params.isActive ? params.isActive : false;

            _curRegion = new MM.EntityCollection();
            _curDragHandles = new MM.EntityCollection();
            _curRectangles = new MM.EntityCollection();
            _curBorders = new MM.EntityCollection();
            _curLabel = new MM.Pushpin(new MM.Location(maxlat, minlon));
            _curRegion.push(_curDragHandles);
            _curRegion.push(_curRectangles);
            _curRegion.push(_curBorders);
            _curRegion.push(_curLabel);

            _curRegion.id = id;
            _curRegion.name = name;
            _curRegion.isActive = isActive;
            addRectangles(minlat, minlon, maxlat, maxlon, isActive);
            addDragHandles();
            setRegionLabel(_curRegion);
            addRegionHandlers(_curRegion);
            _layer.push(_curRegion);

            if (id) _regions[id] = _curRegion;

            return _curRegion;
        };

        /**
         * Toggles Point creation mode.
         */
        this.togglePointMode = function (onModeComplete) {
            if (_mode === "point") {
                _onModeComplete = onModeComplete;
                disableMode();
            } else {
                reset();
                _onModeComplete = onModeComplete;
                FC.Map.disableNavigation();
                enablePointMode();
            }
        };

        /**
         * Toggles Region creation mode.
         */
        this.toggleRegionMode = function (onModeComplete) {
            if (_mode === "region") {
                _onModeComplete = onModeComplete;
                disableMode();
            } else {
                reset();
                _onModeComplete = onModeComplete;
                FC.Map.disableNavigation();
                enableRegionMode();
            }
        };

        this.generatePointName = function () {
            var i = 1, names, name;

            names = Object.keys(_points).map(function (id) {
                return _points[id].name;
            });

            while (names.indexOf(name = "Point " + i) !== -1) i++;

            return name;
        };

        this.generateRegionName = function () {
            var i = 1, names, name;

            names = Object.keys(_regions).map(function (id) {
                return _regions[id].name;
            });

            while (names.indexOf(name = "Region " + i) !== -1) i++;

            return name;
        };

        this.updatePoint = function (id, tile) {
            var point = _points[id];
            _layer.remove(point);
            _points[id] = self.addPoint(tile.latitude, tile.longitude, {
                id: id,
                name: tile.name
            });
        };

        this.updateRegion = function (id, tile) {
            var region = _regions[id],
                minlat = tile.min.latitude,
                minlon = tile.min.longitude,
                maxlat = tile.max.latitude,
                maxlon = tile.max.longitude;

            _layer.remove(region);
            _regions[id] = self.addRegion(minlat, minlon, maxlat, maxlon, {
                id: id,
                name: tile.name
            });

            if (_isActive) {
                proceedCurrentMode();
            }
        };

        this.removePoint = function (id) {
            var point = _points[id];
            delete _points[id];
            _layer.remove(point);
            point.destroy();
        };

        this.removeRegion = function (id) {
            var region = _regions[id];
            delete _regions[id];
            _layer.remove(region);
            region.destroy();
        };

        this.selectPoint = function (id) {
            var point = _points[id],
                pushpin = point.get(0),
                bounds = MM.LocationRect.fromLocations(pushpin.getLocation());

            point.triggerPointClick();
        };

        this.selectRegion = function (id) {
            var region = _regions[id],
                bounds = MM.LocationRect.fromLocations(region.locations);

            region.triggerRegionClick();
        };

        this.unselectPoint = function (id) {
            var point = _points[id];
            if (point) point.triggerOutPointClick();
        };

        this.unselectRegion = function (id) {
            var region = _regions[id];
            if (region) region.triggerOutRegionClick();
        };

        this.removeAll = function () {
            var id;

            for (id in _points) {
                _layer.remove(_points[id]);
                _points[id].destroy();
            }

            for (id in _regions) {
                _layer.remove(_regions[id]);
                _regions[id].destroy();
            }

            _points = {};
            _regions = {};
        };

        this.setView = function (lat, lon) {
            var bounds = MM.LocationRect.fromLocations(new MM.Location(lat, lon));
            _map.setView({ bounds: bounds });
        };

        this.setMapType = function (mapType) {
            var tileLayer;
            _map.setMapType(Microsoft.Maps.MapTypeId.mercator);

            if (mapType instanceof MM.TileSource) {
                tileLayer = new Microsoft.Maps.TileLayer({ mercator: mapType, opacity: 1 });
                if (_map.isCustomTileSource) _layer.removeAt(0);
                _layer.insert(tileLayer, 0);
                _map.isCustomTileSource = true;
            } else {
                if (_map.isCustomTileSource) _layer.removeAt(0);
                _map.setMapType(mapType);
                _map.isCustomTileSource = false;
            }
        };

        this.hideAll = function () {
            var id;

            for (id in _points) {
                _points[id].setOptions({ visible: false });
            }
            for (id in _regions) {
                _regions[id].setOptions({ visible: false });
            }
        };

        this.showAll = function () {
            var id;

            for (id in _points) {
                _points[id].setOptions({ visible: true });
            }
            for (id in _regions) {
                _regions[id].setOptions({ visible: true });
            }
        };

        /**
         * Shows probe.
         */
         this.showProbe = function () {
            if (FC.Controls.Panel.isAnyRightPanelVisible()) return;
            
            _mapProbe.setOptions({ visible: true });

            self.onUpdateProbe();
         };

        /**
         * Hides probe.
         */
        this.hideProbe = function () {
            if (FC.Controls.Panel.isAnyRightPanelVisible()) return;

            _mapProbe.setOptions({ visible: false });

            self.onUpdateProbe();
        };

        /**
         * Shows probe and updates it's data.
         */
        this.updateProbe = function (info) {
            if (FC.Controls.Panel.isAnyRightPanelVisible()) return;

            var lon = info.longitude,
                lat = info.latitude,
                location;

            if (typeof lon === "undefined" || typeof lat === "undefined") {
                location = _mapProbe.getLocation();

                lon = location.longitude;
                lat = location.latitude;
            }

            lon = FC.roundTo(lon, FC.Settings.DISPLAY_PRECISION);
            lat = FC.roundTo(lat, FC.Settings.DISPLAY_PRECISION);

            _mapProbe.setLocation(new MM.Location(lat, lon));
            _mapProbe.setHtmlContent(probeInfoToHtml(info));

            _mapProbe.setOptions({ visible: true });

            // Hack to place infobox entity above d3 layer.
            $(_map.getModeLayer()).find("#geo-map-probe").parent().parent().parent().parent().css("z-index", 1);

            self.onUpdateProbe(lat, lon);
        };

        /**
         * Handler for probe update.
         */
        this.onUpdateProbe = function () {};

        initialize();
    };

    Microsoft.Maps.moduleLoaded("MapEntities");

    return FC;
}(FC || {}, window, jQuery));