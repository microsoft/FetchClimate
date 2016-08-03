var FC = (function (FC, window, $) {
    var _pageNames = ["geography", "layers", "time", "results", "export", "about"];
    var _dataModeValues = ["values", "uncertainty", "provenance"];
    var _timeSeriesAxisValues = ["years", "days", "hours", "all"];

    // Current configuration.
    var _config;

    // Active page: geography | layers | time | results | export | about.
    var _activePage;

    // Array of variable names with displayed variable first or null if not present in hash.
    var _variables = {};

    // Temporal domain.
    var _temporal = null;

    // Time slices.
    var _yearSlice = null;
    var _daySlice = null;
    var _hourSlice = null;

    var _grids = [],
        _points = [],
        _gridId = 0,
        _pointId = 0,
        _selectedVariable = "",
        _dataMode = "values",
        _palette = D3.ColorPalette.parse(FC.Settings.DEFAULT_PALETTE),
        _timeSeriesAxis = "years";

    FC.ClientState = function (service) {
        var self = this;

        // Service object with two methods:
        // getConfiguration() : promise(configuration)
        // performRequest(spatial, temporal) : promise(response)
        var _service = service;

         // Current url hash storing client state.
        var _hash = new FC.Hash(self);

        // Status of the service.
        // Values: "connecting", "connected", "failed: message". 
        var _status = "connected";

        // jQuery-like events (with use of jQuery Callbacks).
        // List of events: "statechange", "statuschange", "hashchange",
        //                 "windowhashchange".
        var _events = {};

        /**
         * Properties.
         */
        Object.defineProperties(self, {
            service: {
                get: function () {
                    return _service;
                }
            },
            hash: {
                get: function () {
                    return _hash;
                }
            },
            status: {
                get: function () {
                    return _status;
                },
                set: function (value) {
                    _status = value;
                    self.trigger("statuschange", _status);
                }
            },
            events: {
                get: function () {
                    return _events;
                }
            },
            config: {
                get: function () {
                    return _config;
                },
                set: function (value) {
                    // NOTE: Set config only if it is not defined.
                    //       Useful for testing.
                    _config = _config || value;
                }
            },
            activePage: {
                get: function () {
                    return _activePage;
                }
            },
            variables: {
                get: function () {
                    return _variables;
                }
            },
            temporal: {
                get: function () {
                    return _temporal;
                }
            },
            yearSlice: {
                get: function () {
                    return _yearSlice;
                }
            },
            daySlice: {
                get: function () {
                    return _daySlice;
                }
            },
            hourSlice: {
                get: function () {
                    return _hourSlice;
                }
            },
            grids: {
                get: function () {
                    return _grids;
                }
            },
            points: {
                get: function () {
                    return _points;
                }
            },
            selectedVariable: {
                get: function () {
                    return _selectedVariable;
                }
            },
            dataMode: {
                get: function () {
                    return _dataMode;
                }
            },
            palette: {
                get: function () {
                    return _palette;
                }
            },
            timeSeriesAxis: {
                get: function () {
                    return _timeSeriesAxis;
                }
            }
        });

        /**
         * Callback function for successful response of load grid (points) data request.
         */
        var onDataReady = function (response, data, statusCallback) {
            var request = response.request;
            var vname = request.variable;
            var spatialRegionType = request.spatial.spatialRegionType;

            var values = data.values;
            var provenance = data.provenance ? 
                 data.provenance : 
                 duplicateConstant(data.values, self.getDataSourceByName(request.dataSources[0]).ID, 65535);
            var uncertainty = data.sd;
            var pointIndex = 0;

            convertInfToNaN(uncertainty);
            setIsAllNaN(values);
            setIsAllNaN(provenance);
            setIsAllNaN(uncertainty);

            if (spatialRegionType === "CellGrid") {
                FC.state.grids.forEach(function (grid) {
                    if (grid.data && grid.data[vname] && grid.data[vname].request && grid.data[vname].request === request) {
                        grid.data[vname].values = values;
                        grid.data[vname].provenance = provenance;
                        grid.data[vname].uncertainty = uncertainty;
                    }
                });
            }
            else if (spatialRegionType === "Points") {
                FC.state.points.forEach(function (point) {
                    if (point.data && point.data[vname] && point.data[vname].request === request) {
                        point.data[vname].values = values[pointIndex];
                        point.data[vname].provenance = provenance[pointIndex];
                        point.data[vname].uncertainty = uncertainty[pointIndex];
                        pointIndex++;
                    }
                });
            }

            request.status = "completed";

            if (statusCallback) {
                statusCallback(request);
            }
        };

        /**
         * Callback function for failed response of load of fetch grid (points) data request.
         */
        var onRequestError = function (request, fault, statusCallback) {
            var vname = request.variable;
            var spatialRegionType = request.spatial.spatialRegionType;
            var elms;
            var hashIdx;

            if (spatialRegionType && spatialRegionType === "Points") {
                elms = _points;
            } else if (spatialRegionType && spatialRegionType === "CellGrid") {
                elms = _grids;
            } else {
                throw "Invalid SpatialRegionType";
            }

            for (var i = 0; i < elms.length; i++) {
                if (elms[i].data) {
                    var data = elms[i].data[vname];
                    if (data && data.request === request) {
                        data.values = data.provenance = data.uncertainty = null;
                    }
                }
            }

            request.status = "failed";
            
            hashIdx = fault.indexOf("hash ");

            if (hashIdx !== -1) {
                request.statusData = fault.substring(hashIdx + 5).trim();
            }

            if (statusCallback) {
                statusCallback(request);
            }


            var errMsg = "Error fetching data for variable " + vname + fault ? fault : "";
            console.log(errMsg);
        };

        var isInfinite = function (value) {
            return !isFinite(value) || value === Number.MAX_VALUE || value === Number.MIN_VALUE;
        };

        var convertInfToNaN = function (data) {
            var i, len, min, max;
            if (!data) return;

            if (data instanceof Array && data.length > 0) {
                len = data.length;
                min = Number.MAX_VALUE;
                max = Number.MIN_VALUE;

                if (data.min && isInfinite(data.min)) data.min = NaN;
                if (data.max && isInfinite(data.max)) data.max = NaN;

                if (data[0] instanceof Array) {
                    for (i = 0; i < len; ++i) {
                        convertInfToNaN(data[i]);
                        if (data[i].min < min) min = data[i].min;
                        if (data[i].max > max) max = data[i].max;
                    }
                } else if (typeof data[0] === "number") {
                    min = Number.MAX_VALUE;
                    max = Number.MIN_VALUE;

                    for (i = 0; i < len; ++i) {
                        if (isInfinite(data[i])) {
                            data[i] = NaN;
                        }

                        if (data[i] < min) min = data[i];
                        if (data[i] > max) max = data[i];
                    }
                }

                data.min = (min === Number.MAX_VALUE) ? NaN : min;
                data.max = (max === Number.MIN_VALUE) ? NaN : max;
            }
        };

        var setIsAllNaN = function (data) {
            var result = true;
            if (!data) return;

            if (data instanceof Array && data.length > 0) {
                if (data[0] instanceof Array) {
                    result = !data.some(function (elem) {
                        return !setIsAllNaN(elem);
                    });
                } else if (typeof data[0] === "number") {
                    result = !data.some(function (elem) {
                        return !isNaN(elem);
                    });
                }

                data.isAllNaN = result;
            }

            return result;
        };

        var duplicateConstant = function (data, val, nan) {
            if (!data)
                return null;

            if (data instanceof Array) {
                if(data.length == 0)
                    return [];
                else {
                    if (data[0] instanceof Array) 
                        return data.map(function (elem) { return duplicateConstant(elem, val, nan); });
                    else {
                        var result = new Array(data.length);
                        for(var i = 0;i<data.length;i++)
                            result[i] = isNaN(data[i]) ? nan : val;
                        return result;
                    }
                }
            }
            else 
                return isNaN(val) ? nan : val;;
        };

        /**
         * Public methods.
         */
        $.extend(self, {
            /**
             * jQuery-like "on" method for events. Attaches a callback to events.
             * @param  {String}   eventString A list of events' names.
             * @param  {Function} callback    A callback to attach to listed events.
             */
            on: function (eventString, callback) {
                var eventNames = eventString.split(" ");
                eventNames.forEach(function (eventName) {
                    var event = _events[eventName];
                    _events[eventName] = event ? event.add(callback) : $.Callbacks().add(callback);
                });
            },

            /**
             * jQuery-like "trigger" method for events. Fires all callbacks
             * for an event with given arguments.
             */
            trigger: function () {
                var eventName = arguments[0];
                var args = Array.prototype.slice.call(arguments, 1);
                var event = _events[eventName];
                _events[eventName] = event ? event.fire.apply(event, args) : $.Callbacks();
            },

            /**
             * jQuery-like "off" method for events. Remove a callback from events.
             * @param  {String}   eventString A list of events' names.
             * @param  {Function} callback    A callback to remove from listed events.
             */
            off: function (eventString, callback) {
                var eventNames = eventString.split(" ");
                eventNames.forEach(function (eventName) {
                    var event = _events[eventName];
                    _events[eventName] = event ? event.remove(callback) : $.Callbacks();
                });
            },

            /**
             * Refreshes configuration using getConfiguration request to the service.
             * @return {Promise} Promise of getConfiguration request.
             */
            refreshConfiguration: function () {
                self.status = "connecting";

                var configPromise = service.getConfiguration({
                    serviceUrl: _service.url
                });

                var uiconfigPromise = $.getJSON("/api/uiconfiguration");


                return $.when(configPromise, uiconfigPromise)
                    .done(function (config, uiconfig) {
                        self.status = "connected";

                        config.boundaries = {};
                        config.boundaries.yearMin = uiconfig[0].Boundaries.Temporal.YearMin;
                        config.boundaries.yearMax = uiconfig[0].Boundaries.Temporal.YearMax;

                        _config = config;
                        _hash.parseState();
                        _hash.update();
                        console.log("[state] config:", _config);                    
                    }).fail(function (error) {
                        self.status = "failed: " + error.responseText;
                        console.log("Error connecting to service: " + error);
                    });
            },

            /**
             * Methods for check data in configuration.
             */
            isVariablePresented: function (variableName) {
                var configVariables = _config && _config.EnvironmentalVariables;
                return configVariables ? !!configVariables.filter(function (variable) {
                    return variable.Name === variableName;
                }).length : true;
            },

            isDataSourcePresented: function (variableName, dataSourceId) {
                var configVariables = _config && _config.EnvironmentalVariables;
                return configVariables ? !!configVariables.filter(function (variable) {
                    var dataSources = variable.DataSources;
                    return variable.Name === variableName && !!dataSources.filter(function (dataSource) {
                        return dataSource.ID === dataSourceId;
                    }).length;
                }).length : true;
            },

            /**
             * Methods for client state change.
             */
            reset: function () {
                _config = null;
                _activePage = null;
                _variables = {};
                _temporal = {};
                _grids = [];
                _points = [];
            },

            setActivePage: function (page, noUpdateHash) {
                if (!page || page === _activePage) return;

                _activePage = (_pageNames.indexOf(page) !== -1) ? page : "geography";

                self.trigger("activepagechange", page);
                self.trigger("statechange", "activePage");
                if (!noUpdateHash) self.hash.update();
            },

            clearVariables: function() {
                _variables = {};
                self.trigger("statechange", "variables");
                self.hash.update();
            },
            
            toggleVariable: function (variableName, dataSources, noUpdateHash) {
                if (!variableName) return;

                dataSources = dataSources || [];

                if (_variables[variableName]) {
                    delete _variables[variableName];

                    _grids.forEach(function (grid) {
                        if (grid.data) {
                            delete grid.data[variableName];
                        }

                        if (grid.heatmap) {
                            grid.heatmap.remove();
                            grid.heatmap = undefined;
                        }
                    });

                    _points.forEach(function (point) {
                        if (point.data) {
                            delete point.data[variableName];
                        }
                    });

                    if (_selectedVariable === variableName) {
                        _selectedVariable = null;
                    }
                } else {
                    _variables[variableName] = {
                        dataSources: dataSources,
                        grids: null,
                        points: null
                    };
                }

                self.trigger("statechange", "variables");
                if (!noUpdateHash) self.hash.update();
            },

            toggleDataSource: function (variableName, dataSourceId, noUpdateHash) {
                if (!variableName || !dataSourceId) return;

                var variable = _variables[variableName];

                if (variable) {
                    var i = variable.dataSources.indexOf(dataSourceId);

                    if (i !== -1) {
                        variable.dataSources.splice(i, 1);
                    } else {
                        variable.dataSources.push(dataSourceId);
                    }

                    _grids = _grids.map(function (grid) {
                        if (grid.data) grid.data[variableName] = {};

                        return grid;
                    });

                    _points = _points.map(function (point) {
                        if (point.data) point.data[variableName] = {};

                        return point;
                    });

                    self.trigger("statechange", "dataSources");
                    if (!noUpdateHash) self.hash.update();
                }
            },

            setTemporal: function (temporal, noUpdateHash) {
                if (!temporal || (_temporal && _temporal.isEqual(temporal))) return;

                _temporal = $.extend(true, {}, temporal);

                if (!noUpdateHash) {
                    _yearSlice = (_temporal.yearCellMode) ? (_temporal.years.length > 2) ? 0 : null : null;
                    _daySlice = (_temporal.dayCellMode) ? (_temporal.days.length > 2) ? 0 : null : null;
                    _hourSlice = (_temporal.hourCellMode) ? (_temporal.hours.length > 2) ? 0 : null : null;
                }

                _grids = _grids.map(function (grid) {
                    grid.data = {};
                        
                    if (grid.heatmap) {
                        grid.heatmap.remove();
                        grid.heatmap = undefined;
                    }

                    return grid;
                });

                _points = _points.map(function (point) {
                    point.data = {};

                    return point;
                });

                self.trigger("statechange", "temporal");
                if (!noUpdateHash) self.hash.update();
            },

            setTimeSlice: function (yearSlice, daySlice, hourSlice, noUpdateHash) {
                if (_yearSlice !== yearSlice ||
                    _daySlice !== daySlice ||
                    _hourSlice !== hourSlice) {

                    _yearSlice = yearSlice;
                    _daySlice = daySlice;
                    _hourSlice = hourSlice;

                    self.trigger("statechange", "slice");
                    if (!noUpdateHash) self.hash.update();
                }
            },

            setYearSlice: function (yearSlice, noUpdateHash) {
                if (_yearSlice !== yearSlice) {
                    _yearSlice = yearSlice;

                    self.trigger("statechange", "slice");
                    if (!noUpdateHash) self.hash.update();
                }
            },

            setDaySlice: function (daySlice, noUpdateHash) {
                if (_daySlice !== daySlice) {
                    _daySlice = daySlice;

                    self.trigger("statechange", "slice");
                    if (!noUpdateHash) self.hash.update();
                }
            },

            setHourSlice: function (hourSlice, noUpdateHash) {
                if (_hourSlice !== hourSlice) {
                    _hourSlice = hourSlice;

                    self.trigger("statechange", "slice");
                    if (!noUpdateHash) self.hash.update();
                }
            },

            setGrids: function (grids, noUpdateHash) {
                if (!grids.length) {
                    FC.Map.clearAllHeatmaps();
                }

                _grids = grids;

                self.trigger("statechange", "grids");
                if (!noUpdateHash) self.hash.update();
            },

            addGrid: function (grid, noUpdateHash) {
                grid.id = ++_gridId;
                _grids.push(grid);

                self.trigger("statechange", "grids");
                if (!noUpdateHash) self.hash.update();

                return _gridId;
            },

            setGrid: function (id, grid, noUpdateHash) {
                if (typeof id === "undefined") {
                    return;
                }

                _grids = _grids.map(function (_grid) {
                    if (_grid.id === id) {
                        if (grid.isEqual(_grid)) {
                            grid.data = _grid.data;
                        }
                        else if (_grid.heatmap) {
                            _grid.heatmap.remove();
                            _grid.heatmap = undefined;
                        }

                        grid.id = id;
                        return grid;
                    }
                    else {
                        return _grid;
                    }
                });

                self.trigger("statechange", "grids");
                if (!noUpdateHash) self.hash.update();
            },

            deleteGrid: function (id, noUpdateHash) {
                _grids.map(function (_grid) {
                    if (_grid.id === id && _grid.heatmap) {
                        _grid.heatmap.remove();
                        _grid.heatmap = undefined;
                    }
                });

                _grids = _grids.filter(function (_grid) {
                    return id !== _grid.id;
                });

                self.trigger("statechange", "grids");
                if (!noUpdateHash) self.hash.update();
            },

            setPoints: function (points, noUpdateHash) {
                if (!points.length) {
                    FC.Map.clearPointMarkers();
                }

                _points = points;

                self.trigger("statechange", "points");
                if (!noUpdateHash) self.hash.update();
            },

            addPoint: function (point, noUpdateHash) {
                point.id = ++_pointId;
                _points.push(point);

                self.trigger("statechange", "points");
                if (!noUpdateHash) self.hash.update();

                return _pointId;
            },

            setPoint: function (id, point, noUpdateHash) {
                if (typeof id === "undefined") {
                    return;
                }

                _points = _points.map(function (_point) {
                    if (_point.id === id) {
                        if (point.isEqual(_point)) {
                            point.data = _point.data;
                        }

                        point.id = id;
                        return point;
                    }
                    else {
                        return _point;
                    }
                });

                self.trigger("statechange", "points");
                if (!noUpdateHash) self.hash.update();
            },

            deletePoint: function (id, noUpdateHash) {
                _points = _points.filter(function (_point) {
                    return id !== _point.id;
                });

                if (FC.Map.markersPlot) {
                    FC.Map.markersPlot.remove();
                    FC.Map.markersPlot = undefined;
                }

                self.trigger("statechange", "points");
                if (!noUpdateHash) self.hash.update();
            },

            selectVariable: function (vname) {
                _selectedVariable = vname;

                self.trigger("statechange", "selectedvariable");
            },

            setDataMode: function (dataMode, noUpdateHash) {
                if (!dataMode || dataMode === _dataMode) return;

                dataMode = dataMode.toLowerCase();
                _dataMode = (_dataModeValues.indexOf(dataMode) !== -1) ? dataMode : "values";

                self.trigger("statechange", "dataMode");
                if (!noUpdateHash) self.hash.update();
            },

            setPalette: function (palette) {
                _palette = palette;

                self.trigger("statechange", "palette");
            },

            setPaletteRange: function (range) {
                _palette = _palette.absolute(range.min, range.max);
            },

            setTimeSeriesAxis: function (timeSeriesAxis, noUpdateHash) {
                if (!timeSeriesAxis || timeSeriesAxis === _timeSeriesAxis) return;

                timeSeriesAxis = timeSeriesAxis.toLowerCase();
                _timeSeriesAxis = (_timeSeriesAxisValues.indexOf(timeSeriesAxis) !== -1) ? timeSeriesAxis : "years";

                self.trigger("statechange", "timeSeriesAxis");
                if (!noUpdateHash) self.hash.update();
            },

            /**
             * Fetches missed data for variable wih given name and particular data sources if presented.
             *
             * @param   {string}    vname            A name of variable to fetch.
             * @param   {array}     dataSources      An array of ids of particular data sources.
             * @param   {function}  statusCallback   A callback function to call when status of request changed.
             */
            fetchVariable: function (vname, dataSources, statusCallback) {
                var pointsToFetch = []; // array of points to fetch

                _grids.forEach(function (grid) {
                    if (!grid.data) {
                        grid.data = {};
                    }

                    if (!grid.data[vname]) {
                        grid.data[vname] = {
                            request: null,
                            values: null,
                            provenance: null,
                            uncertainty: null
                        };
                    }
 
                    // Grid is not requested yet.
                    if (!grid.data[vname].request) {
                        var request = new FC.Request({
                            spatial: new FC.CellGrid(
                                    grid.latmin,
                                    grid.latmax,
                                    grid.latcount,
                                    grid.lonmin,
                                    grid.lonmax,
                                    grid.loncount
                                ),
                            temporal: self.temporal,
                            variable: vname,
                            serviceUrl: self.service.url,
                            dataSources: dataSources ? dataSources : null
                        });

                        grid.data[vname].request = request;

                        request.perform(statusCallback).then(
                            function (response) {
                                response.getData()
                                    .then(
                                        function (data) { onDataReady(response, data, statusCallback); },
                                        function (error) { onRequestError(request, error, statusCallback); }
                                    );
                            },
                            function (error) {
                                onRequestError(request, error, statusCallback);
                            }
                        );
                    }
                });

                // Find points to be fetched.
                _points.forEach(function (point) {
                    if (!point.data) {
                        point.data = {};
                    }

                    if (!point.data[vname]) {
                        point.data[vname] = {
                            request: null,
                            values: null,
                            provenance: null,
                            uncertainty: null
                        };
                    }
 
                    // Point is not requested yet.
                    if (!point.data[vname].request) {
                        pointsToFetch.push(point);
                    }
                });

                // Fetch all points in one request if any.
                if (pointsToFetch.length) {
                    var request = new FC.Request({
                        spatial: new FC.ScatteredPoints(pointsToFetch),
                        temporal: self.temporal,
                        variable: vname,
                        serviceUrl: self.service.url,
                        dataSources: dataSources ? dataSources : null
                    });

                    pointsToFetch.forEach(function (point) {
                        point.data[vname].request = request;
                    });

                    request.perform(statusCallback).then(
                        function (response) {
                            response.getData()
                                .then(
                                    function (data) { onDataReady(response, data, statusCallback); },
                                    function (error) { onRequestError(request, error, statusCallback); }
                                );
                        },
                        function (error) {
                            onRequestError(request, error, statusCallback);
                        }
                    );
                }
            },

            /**
             * Fetches missed grids and/or points data for every selected variable.
             *
             * @param {function} statusCallback     A callback function to call when status of request changed. 
             */
            fetchMissingData: function (statusCallback) {
                if (Object.keys(_variables).length > 0) {
                    Object.keys(_variables).forEach(function (variable) {
                        var dataSources = [];

                        _variables[variable].dataSources.forEach(function (id) {
                            dataSources.push(FC.state.getDataSourceById(id).Name);
                        });

                        self.fetchVariable(variable, dataSources, statusCallback);
                    });
                }
            },

            /**
             * Is there any data to fetch?
             * @return {Boolean} 
             */
            anyPendingRequest: function () {
                var any = false,
                    hasPendingRequest,
                    variable, i;

                for (variable in _variables) {
                    for (i = 0; i < _grids.length; ++i) {
                        any = any || !(_grids[i].data && _grids[i].data[variable] &&
                              _grids[i].data[variable].request);
                    }

                    for (i = 0; i < _points.length; ++i) {
                        any = any || !(_points[i].data && _points[i].data[variable] &&
                              _points[i].data[variable].request);
                    }
                }

                return any;
            },

            /**
             * Returns status of variable. Possible values:
             *
             *  - 'not requested'   data for this variable is not requested yet;
             *
             *  - 'queued'          if none of requests is calculating yet, but every request is
             *                      queued. Also returns position in queue which is the lowest queued
             *                      number among all requests for this variable;
             *
             *  - 'calculating'     if at least one of requests is calculating now. Also returns progress in
             *                      % which is equal to SUM(progress of every request) / (number of requests).
             *                      Progress for request:
             *                          - 100% for failed or completed;
             *                          - 0% for queued;
             *                          - percent of calculation progress for calculating.
             *
             *  - 'receiving'       if at least one of requests is receiving and no other request is calculating;
             *
             *  - 'completed'       if data for this variable was fetched for every grid and point;
             *
             *
             *  - 'failed'          if at least one of requests failed and all other requests are completed.
             *                      Also returns hashes for failed requests.
             *
             * @param {string}  vname   A name of variable which status to get.
             */
            getVariableStatus: function (vname) {
                var totalProgress = 0,
                    positionInQueue,
                    requests = [],
                    failedRequests = [], // hashes of failed requests
                    calculatingCount = 0,
                    receivingCount = 0;

                _grids.forEach(function (grid) {
                    if (grid.data && grid.data[vname]) {
                        var request = grid.data[vname].request;

                        // One request - one grid.
                        if (request) {
                            requests.push(request);
                        }
                    }
                });

                _points.forEach(function (point) {
                    if (point.data && point.data[vname]) {
                        var request = point.data[vname].request;

                        // One request - multiple points.
                        if (request && requests.indexOf(request) === -1) {
                            requests.push(request);
                        }
                    }
                });

                requests.forEach(function (request) {
                    switch (request.status) {
                        case "new":
                            break;
                        case "pending":
                            // Default value for positioninQueue is NaN.
                            positionInQueue = Math.min(positionInQueue, request.positionInQueue) || request.positionInQueue;
                            break;
                        case "calculating":
                            totalProgress += request.percentCompleted;
                            calculatingCount++;
                            break;
                        case "receiving":
                            totalProgress += 100;
                            receivingCount++;
                            break;
                        case "completed":
                            totalProgress += 100;
                            break;
                        case "failed":
                            totalProgress += 100;
                            failedRequests.push(request.hash);
                            break;
                    }
                });

                // No request is calculating or calculated.
                if (totalProgress === 0 && calculatingCount === 0) {
                    // No queued requests.
                    if (!positionInQueue) {
                        return "Not requested";
                    }
                    else {
                        return "Queued " + positionInQueue;
                    }
                }
                // No calculating requests at this moment, some requests calculated already.
                else if (calculatingCount === 0) {
                    // Some requests receive data.
                    if  (receivingCount > 0) {
                        return "Receiving";
                    }
                    // All requests were calculated, no requests receiving data.
                    else {
                        if (totalProgress / requests.length === 100 && !failedRequests.length) {
                            return "Completed";
                        }
                        else if (totalProgress / requests.length === 100 && failedRequests.length) {
                            return "Failed " + failedRequests.join(",");
                        }
                        // Some requests are not yet handled by server and are pending.
                        else {
                            return "Progress " + FC.roundTo(totalProgress / requests.length, 0) + " %";
                        }
                    }
                }
                // Some request is calculating.
                else if (calculatingCount > 0 || (totalProgress > 0 && receivingCount === 0)) {
                    return "Progress " + totalProgress / requests.length + " %";
                }
                else {
                    console.log("[UNEXPECTED STATUS] vname: %s, requests: %d, progress: %d, queued: %d, recevingCount: %d, calculatingCount: %d",
                        vname, +requests.length, +(totalProgress / requests.length), +positionInQueue, +receivingCount, +calculatingCount);
                    return "Unexpected status";
                }
            },

            hasAnyCompletedData: function () {
                var sv = _selectedVariable;
                var dm = _dataMode;
                var result = false;

                result = result || _points.some(function (point) {
                    return point.data && point.data[sv] && point.data[sv][dm];
                });

                result = result || _grids.some(function (grid) {
                    return grid.data && grid.data[sv] && grid.data[sv][dm];
                });

                return result;
            },

            hasAllNaNData: function () {
                var sv = _selectedVariable;
                var dm = _dataMode;
                var result = true;

                _points.forEach(function (point) {
                    if (point.data && point.data[sv] && point.data[sv][dm]) {
                        result = result && point.data[sv][dm].isAllNaN;
                    }
                });

                _grids.forEach(function (grid) {
                    if (grid.data && grid.data[sv] && grid.data[sv][dm]) {
                        result = result && grid.data[sv][dm].isAllNaN;
                    }
                });

                return result;
            },

            getGridData: function (grid, options) {
                var data,
                    lat, lon,
                    ys, ds, hs, dm,
                    sv = _selectedVariable;

                options = options || {};
                ys = options.yearSlice;
                ds = options.daySlice;
                hs = options.hourSlice;
                dm = options.dataMode;

                // Fill data with NaN values to show empty heatmap while fetching data for variable or if no variable is selected.
                if ($.isEmptyObject(_variables) || self.getVariableStatus(sv).match(/not requested/) ||
                    !grid.data || !grid.data[sv] || !grid.data[sv][dm]) {
                    data = new Array(grid.loncount - 1);

                    for (lon = 0; lon < grid.loncount - 1; lon++) {
                        data[lon] = new Array(grid.latcount - 1);
                        for (lat = 0; lat < grid.latcount - 1; lat++) {
                            data[lon][lat] = Number.NaN;
                        }
                    }

                    return data;
                }

                data = new Array(grid.data[sv][dm].length);
                for (lon = 0; lon < grid.data[sv][dm].length; lon++) {
                    data[lon] = new Array(grid.data[sv][dm][lon].length);

                    for (lat = 0; lat < grid.data[sv][dm][lon].length; lat++) {
                        var slice = grid.data[sv][dm][lon][lat];
                        
                        if (ys !== null) slice = slice[ys];
                        if (ds !== null) slice = slice[ds];
                        if (hs !== null) slice = slice[hs];

                        data[lon][lat] = slice;
                    }
                }

                return data;
            },

            getPointsData: function (points, options) {
                var data = {
                        x: [],
                        y: [],
                        f: []
                    },
                    i, ys, ds, hs, dm,
                    sv = _selectedVariable;

                options = options || {};
                ys = options.yearSlice;
                ds = options.daySlice;
                hs = options.hourSlice;
                dm = options.dataMode;

                // Fill data with NaN values to show empty marker if no variable is selected.
                if ($.isEmptyObject(_variables) || self.getVariableStatus(sv).match(/not requested/)) {
                    for (i = 0; i < points.length; i++) {
                        data.x.push(points[i].lon);
                        data.y.push(points[i].lat);
                        data.f.push(Number.NaN);
                    }

                    return data;
                }

                points.forEach(function (point) {
                    var v;

                    data.x.push(point.lon);
                    data.y.push(point.lat);

                    // Fill data with NaN values to show empty marker while fetching data for this marker.
                    if (!point.data || !point.data[sv] || !point.data[sv][dm]) {
                        v = Number.NaN;
                    } else {
                        v = point.data[sv][dm];

                        if (ys !== null) v = v[ys];
                        if (ds !== null) v = v[ds];
                        if (hs !== null) v = v[hs];
                    }

                    data.f.push(v);
                });

                return data;
            },

            /**
             * Gets range of values for given variable in current data mode.
             */
            getVariableDataRange: function (vname, dataMode) {
                var min, max, i, data;

                vname = vname || _selectedVariable;
                dataMode = dataMode || _dataMode;

                min = Number.MAX_VALUE;
                max = -Number.MAX_VALUE;

                // scan points
                for (i = 0; i < _points.length; i++) {
                    if (_points[i].data && _points[i].data[vname] && _points[i].data[vname][dataMode]) {
                        data = _points[i].data[vname][dataMode];
                        if (data.min < min) min = data.min;
                        if (data.max > max) max = data.max;
                    }
                }

                // scan grids
                for (i = 0; i < _grids.length; i++) {
                    if (_grids[i].data && _grids[i].data[vname] && _grids[i].data[vname][dataMode]) {
                        data = _grids[i].data[vname][dataMode];
                        if (data.min < min) min = data.min;
                        if (data.max > max) max = data.max;
                    }
                }

                return (min <= max) ? { min: min, max: max } : { min: Number.NaN, max: Number.NaN };
            },

            getPointDataCube: function () {
                var result = [];
                var sv = _selectedVariable;
                var dm = _dataMode;

                _points.forEach(function (point) {
                    if (point.data && point.data[sv] && point.data[sv][dm]) {
                        result.push(point.data[sv][dm]);
                    } else {
                        return null;
                    }
                });

                return result;
            },

            getGridDataCube: function (grid) {
                var sv = _selectedVariable;
                var dm = _dataMode;

                if (grid.data && grid.data[sv] && grid.data[sv][dm]) {
                    return grid.data[sv][dm];
                } else {
                    return null;
                }
            },

            getVariableUnits: function (variable) {
                variable = variable || _selectedVariable;
                return variable ? FC.state.config.EnvironmentalVariables.filter(function (v) {
                    return v.Name === variable;
                })[0].Units : "";
            },

            getDataSourceById: function (id) {
                var dataSource = FC.state.config.DataSources.filter(function (ds) {
                    return ds.ID === id;
                })[0];

                return dataSource;
            },

            getDataSourceByName: function (name) {
                var dataSource = FC.state.config.DataSources.filter(function (ds) {
                    return ds.Name === name;
                })[0];

                return dataSource;
            },

            getSelectedEnvironmentalVariable: function () {
                var variable = FC.state.config.EnvironmentalVariables.filter(function (_variable) {
                    return _variable.Name === FC.state.selectedVariable;
                });

                return variable[0];
            },

            getTimeSeriesDataForPoint: function (point, mode) {
                var yearSlice, daySlice, hourSlice;
                var timeSeriesData = null;
                var td = _temporal;
                var sv = _selectedVariable;
                var dm = mode || _dataMode;
                var tsa = _timeSeriesAxis;

                // Data values.
                var data, value;

                // Data for time series graph.
                var xData = [];
                var yData = [];

                // Counters.
                var y, d, h;

                // Index bounds.
                var ys, ye, ds, de, hs, he;

                var getAxisLabel = function () {
                    if (tsa === "years") return y;
                    else if (tsa === "days") return d;
                    else if (tsa === "hours") return h;
                };

                ys = ds = hs = 0;
                ye = de = he = 1;

                if (sv && dm && tsa) {
                    switch (tsa) {
                        case "years":
                            ye = Math.max(1, td.yearCellMode ? td.years.length - 1 : td.years.length);
                            break;
                        case "days":
                            de = Math.max(1, td.dayCellMode ? td.days.length - 1 : td.days.length);
                            break;
                        case "hours":
                            he = Math.max(1, td.hourCellMode ? td.hours.length - 1 : td.hours.length);
                            break;
                    }

                    if (point.data && point.data[sv] && point.data[sv][dm]) {
                        data = point.data[sv][dm];

                        for (y = ys; y < ye; y++) {
                            for (d = ds; d < de; d++) {
                                for (h = hs; h < he; h++) {
                                    yearSlice = (tsa === "years") ? y : _yearSlice;
                                    daySlice = (tsa === "days") ? d : _daySlice;
                                    hourSlice = (tsa === "hours") ? h : _hourSlice;
                                    value = data;

                                    if (yearSlice !== null) value = value[yearSlice];
                                    if (daySlice !== null) value = value[daySlice];
                                    if (hourSlice !== null) value = value[hourSlice];

                                    if (!isNaN(value) && isFinite(value)) {
                                        xData.push(getAxisLabel());
                                        yData.push(value);
                                    }
                                }
                            }
                        }

                        timeSeriesData = {
                            location: "Point",
                            x: xData,
                            y: yData
                        };
                    }
                }

                return timeSeriesData;
            },

            getTimeSeriesDataForGridPoint: function (grid, clat, clon, mode) {
                var yearSlice, daySlice, hourSlice;
                var timeSeriesData = null;
                var td = _temporal;
                var sv = _selectedVariable;
                var dm = mode || _dataMode;
                var tsa = _timeSeriesAxis;

                // Data values.
                var data, value;

                // Data for time series graph.
                var xData = [];
                var yData = [];

                // Counters.
                var y, d, h;
                var lat, lon;

                // Index bounds.
                var ys, ye, ds, de, hs, he;
                var lats, late, lons, lone;

                var getAxisLabel = function () {
                    if (tsa === "years") return y;
                    else if (tsa === "days") return d;
                    else if (tsa === "hours") return h;
                };

                ys = ds = hs = lats = lons = 0;
                ye = de = he = 1;

                if (sv && dm && tsa) {
                    switch (tsa) {
                        case "years":
                            ye = Math.max(1, td.yearCellMode ? td.years.length - 1 : td.years.length);
                            break;
                        case "days":
                            de = Math.max(1, td.dayCellMode ? td.days.length - 1 : td.days.length);
                            break;
                        case "hours":
                            he = Math.max(1, td.hourCellMode ? td.hours.length - 1 : td.hours.length);
                            break;
                    }

                    if (grid.data && grid.data[sv] && grid.data[sv][dm]) {
                        data = grid.data[sv][dm];
                        lone = data.length;
                        late = data[0].length;

                        for (y = ys; y < ye; y++) {
                            for (d = ds; d < de; d++) {
                                for (h = hs; h < he; h++) {
                                    for (lon = lons; lon < lone; lon++) {
                                        for (lat = lats; lat < late; lat++) {
                                            yearSlice = (tsa === "years") ? y : _yearSlice;
                                            daySlice = (tsa === "days") ? d : _daySlice;
                                            hourSlice = (tsa === "hours") ? h : _hourSlice;
                                            value = data[lon][lat];

                                            if (yearSlice !== null) value = value[yearSlice];
                                            if (daySlice !== null) value = value[daySlice];
                                            if (hourSlice !== null) value = value[hourSlice];

                                            if (!isNaN(value) && isFinite(value) && lat === clat && lon === clon) {
                                                xData.push(getAxisLabel());
                                                yData.push(value);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        timeSeriesData = {
                            location: "Grid",
                            x: xData,
                            y: yData
                        };
                    }
                }

                return timeSeriesData;
            },

            /**
             * Returns min/max timeseries for all data.
             */
            getTimeSeriesDataForAll: function () {
                var timeSeriesData = null;
                var td = _temporal;
                var sv = _selectedVariable;
                var dm = _dataMode;
                var tsa = _timeSeriesAxis;
                
                // Data values.
                var data, value, min, max;

                // Data for time series graph.
                var xData = [];
                var yMin = [];
                var yMax = [];

                // Counters.
                var y, d, h, k;
                var lat, lon;

                // Index bounds.
                var ys, ye, ds, de, hs, he;
                var lats, late, lons, lone;

                // Values for each fixed slice value of current time series axis.
                var sliceValues = {};

                ys = ds = hs = 0;
                ye = Math.max(1, td.yearCellMode ? td.years.length - 1 : td.years.length);
                de = Math.max(1, td.dayCellMode ? td.days.length - 1 : td.days.length);
                he = Math.max(1, td.hourCellMode ? td.hours.length - 1 : td.hours.length);

                if (sv && dm && tsa) {
                    _points.forEach(function (point) {
                        if (point.data && point.data[sv] && point.data[sv][dm]) {
                            data = point.data[sv][dm];
                            
                            for (y = ys; y < ye; y++) {
                                for (d = ds; d < de; d++) {
                                    for (h = hs; h < he; h++) {
                                        value = data;

                                        if (_yearSlice !== null) value = value[y];
                                        if (_daySlice !== null) value = value[d];
                                        if (_hourSlice !== null) value = value[h];

                                        if (!isNaN(value) && isFinite(value)) {
                                            if (tsa === "years") k = y;
                                            if (tsa === "days") k = d;
                                            if (tsa === "hours") k = h;

                                            sliceValues[k] = sliceValues[k] || {};
                                            sliceValues[k].max = sliceValues[k].max || Number.MIN_VALUE;
                                            sliceValues[k].min = sliceValues[k].min || Number.MAX_VALUE;

                                            if (value > sliceValues[k].max) sliceValues[k].max = value;
                                            if (value < sliceValues[k].min) sliceValues[k].min = value;
                                        }
                                    }
                                }
                            }
                        }
                    });

                    _grids.forEach(function (grid) {
                        if (grid.data && grid.data[sv] && grid.data[sv][dm]) {
                            data = grid.data[sv][dm];
                            lats = lons = 0;
                            late = data.length;
                            lone = data[0].length;

                            for (y = ys; y < ye; y++) {
                                for (d = ds; d < de; d++) {
                                    for (h = hs; h < he; h++) {
                                        for (lat = lats; lat < late; lat++) {
                                            for (lon = lons; lon < lone; lon++) {
                                                value = data[lat][lon];

                                                if (_yearSlice !== null) value = value[y];
                                                if (_daySlice !== null) value = value[d];
                                                if (_hourSlice !== null) value = value[h];

                                                if (!isNaN(value) && isFinite(value)) {
                                                    if (tsa === "years") k = y;
                                                    if (tsa === "days") k = d;
                                                    if (tsa === "hours") k = h;

                                                    sliceValues[k] = sliceValues[k] || {};
                                                    sliceValues[k].max = sliceValues[k].max || Number.MIN_VALUE;
                                                    sliceValues[k].min = sliceValues[k].min || Number.MAX_VALUE;

                                                    if (value > sliceValues[k].max) sliceValues[k].max = value;
                                                    if (value < sliceValues[k].min) sliceValues[k].min = value;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    });

                    for (var key in sliceValues) {
                        min = sliceValues[key].min;
                        max = sliceValues[key].max;
                        yMin.push(min);
                        yMax.push(max);
                    }

                    xData = Object.keys(sliceValues).map(function (key) {
                        return +key;
                    });

                    timeSeriesData = {
                        location: "MinMax Range",
                        x: xData,
                        yMin: yMin,
                        yMax: yMax
                    };
                }

                return timeSeriesData;
            },
        });
    };

    /**
     * Hash parameters and their URL encodings.
     * [Page]      page=geography|layers|time|results|export|about;
     * [Variables] v=name1,name2,...,nameN;
     * [Temporal]  yc=values|y=values&dc=values|d=values&hc=values|h=values,
     *             where values=t1,t2,...,tN - individual time points,
     *             or values=t1:k:t2 - MatLab range with step k;
     * [Grids]     g=latmin1,latmax1,latcount1,lonmin1,lonmax1,loncount1,name1,...,
     *               latminN,latmaxN,latcountN,lonminN,lonmaxN,loncountN,nameN;
     * [Points]    p=lat1,lon1,name1,...,latN,lonN,nameN.
     */
    FC.Hash = function (state) {
        var self = this;
        var _state = state;
        var _hashString = "";

        /**
         * Properties.
         */
        Object.defineProperties(self, {
            hashString: {
                get: function () {
                    return _hashString;
                }
            }
        });

        /**
         * Hash string construction functions.
         */
        function addParameter(param) {
            if (!param) return;

            var isFirst = _hashString.length === 0;
            _hashString += isFirst ? param + "=" : "&" + param + "=";
        }

        function addValue(value, isFirst) {
            if (!value) return;

            isFirst = typeof isFirst === "undefined" ? true : isFirst;
            _hashString += isFirst ? value : "," + value;
        }

        function addArray(array) {
            if (!array || array.length === 0) return;

            _hashString += "(";
            for (var i = 0; i < array.length; ++i) {
                _hashString += array[i] + ",";
            }
            _hashString = _hashString.slice(0, -1);
            _hashString += ")";
        }

        function addTemporal(temporal) {
            var isFirst = _hashString.length === 0;
            var tdb = new FC.TemporalDomainBuilder();
            var temporalString = tdb.getTemporalDomainString(temporal);
            _hashString += isFirst ? temporalString : "&" + temporalString;
        }

        /**
         * Parsing functions.
         */
        function parseActivePage(page) {
            page = decodeURIComponent(page);
            _state.setActivePage(page, true);
        }

        function parseVariables(variables) {
            var variableStrings = variables.match(/\w+\([\w,]+\)|\w+/gi);
            var decodeVariable = function (variableName) {
                return _state.isVariablePresented(variableName) ?
                       decodeURIComponent(variableName) : null;
            };
            var decodeDataSource = function (dataSourceId) {
                return _state.isDataSourcePresented(variableName, +dataSourceId) ? 
                       +decodeURIComponent(dataSourceId) : null;
            };
            var filterDataSource = function (dataSourceId) {
                return !!dataSourceId;
            };

            for (var i = 0; i < variableStrings.length; ++i) {
                var variableParts = variableStrings[i].split(/[\(,\)]/);
                var variableName = decodeVariable(variableParts[0]);
                var dataSources = variableParts.slice(1, variableParts.length - 1)
                                               .map(decodeDataSource)
                                               .filter(filterDataSource);

                _state.toggleVariable(variableName, dataSources, true);
            }
        }

        function parseGrids(grids) {
            _grids = [];
            var gridParams = grids.split(",");
            var idx = 0;
            try {
                while (idx < gridParams.length - 6) // To make sure all 7 items are in bound
                    FC.state.addGrid(new FC.GeoGrid(
                        parseFloat(gridParams[idx++]),
                        parseFloat(gridParams[idx++]),
                        parseFloat(gridParams[idx++]),
                        parseFloat(gridParams[idx++]),
                        parseFloat(gridParams[idx++]),
                        parseFloat(gridParams[idx++]),
                        decodeURIComponent(gridParams[idx++])),
                        true);
            }
            catch (e) {
                console.log("Error parsing 'g' option: " + e);
            }
        }

        function parsePoints(points) {
            _points = [];
            var pointParams = points.split(",");
            var idx = 0;
            try {
                while (idx < pointParams.length - 2) // To make sure all 3 items are in bound
                    FC.state.addPoint(new FC.GeoPoint(
                        parseFloat(pointParams[idx++]),
                        parseFloat(pointParams[idx++]),
                        decodeURIComponent(pointParams[idx++])),
                        true);
            }
            catch (e) {
                console.log("Error parsing 'p' option: " + e);
            }
        }

        function parseYearSlice(yearSlice) {
            yearSlice = +decodeURIComponent(yearSlice);
            _state.setYearSlice(yearSlice, true);
        }

        function parseDaySlice(daySlice) {
            daySlice = +decodeURIComponent(daySlice);
            _state.setDaySlice(daySlice, true);
        }

        function parseHourSlice(hourSlice) {
            hourSlice = +decodeURIComponent(hourSlice);
            _state.setHourSlice(hourSlice, true);
        }

        function parseDataMode(dataMode) {
            dataMode = decodeURIComponent(dataMode);
            _state.setDataMode(dataMode, true);
        }

        function parseTimeSeriesAxis(timeSeriesAxis) {
            timeSeriesAxis = decodeURIComponent(timeSeriesAxis);
            _state.setTimeSeriesAxis(timeSeriesAxis, true);
        }

        /**
         * Public methods.
         */
        $.extend(self, {
            /**
             * Parse state from URL hash.
             */
            parseState: function () {
                // Get new hash.
                var newHashString = location.hash.substring(1);
                var tdb = new FC.TemporalDomainBuilder();

                // Clear state.
                _activePage = _activePage || "geography";
                _variables = {};
                _temporal = null;
                _dataMode = _dataMode || "values";

                // Get parts of hash parameters.
                var parts = newHashString.split("&");

                // Parsing.
                for (var i = 0; i < parts.length; i++) {
                    var paramValue = parts[i].split("=");
                    var param = paramValue[0];
                    var value = paramValue[1];

                    switch (param) {
                        case "page": parseActivePage(value);     break;
                        case    "v": parseVariables(value);      break;
                        case   "yc": tdb.parseYearCells(value);  break;
                        case    "y": tdb.parseYearPoints(value); break;
                        case   "dc": tdb.parseDayCells(value);   break;
                        case    "d": tdb.parseDayPoints(value);  break;
                        case   "hc": tdb.parseHourCells(value);  break;
                        case    "h": tdb.parseHourPoints(value); break;
                        case    "g": parseGrids(value);          break;
                        case    "p": parsePoints(value);         break;
                        case   "ys": parseYearSlice(value);      break;
                        case   "ds": parseDaySlice(value);       break;
                        case   "hs": parseHourSlice(value);      break;
                        case   "dm": parseDataMode(value);       break;
                        case    "t": parseTimeSeriesAxis(value); break;
                    }
                }

                _state.setTemporal(tdb.getTemporalDomain(), true);
            },

            /**
             * Synchronizes hash with client state.
             */
            update: function () {
                _hashString = "";

                // Set active page parameter.
                if (_activePage) {
                    addParameter("page");
                    addValue(_activePage);
                }

                // Set data mode parameter.
                if (_dataMode) {
                    addParameter("dm");
                    addValue(_dataMode);
                }

                // Set time series axis parameter.
                if (_timeSeriesAxis) {
                    addParameter("t");
                    addValue(_timeSeriesAxis);
                }

                // Set variables parameter.
                if (_variables && Object.keys(_variables).length > 0) {
                    addParameter("v");
                    Object.keys(_variables).forEach(function (variableName, idx) {
                        if (_variables.hasOwnProperty(variableName)) {
                            addValue(variableName, idx === 0);
                            addArray(_variables[variableName].dataSources);
                        }
                    });
                }

                // Set temporal domain.
                if (_temporal) {
                    addTemporal(_temporal);
                }

                // Set grids parameter.
                if (_grids.length) {
                    addParameter("g");
                     Object.keys(_grids).forEach(function (key, idx) {
                        if (_grids.hasOwnProperty(key)) {
                            addValue(_grids[key].toString(), idx === 0);
                        }
                    });
                }

                // Set points parameter.
                if (_points.length) {
                    addParameter("p");
                      Object.keys(_points).forEach(function (key, idx) {
                        if (_points.hasOwnProperty(key)) {
                            addValue(_points[key].toString(), idx === 0);
                        }
                    });
                }

                // Set year slice parameter.
                if (_yearSlice) {
                    addParameter("ys");
                    addValue(_yearSlice);
                }

                // Set day slice parameter.
                if (_daySlice) {
                    addParameter("ds");
                    addValue(_daySlice);
                }

                // Set hour slice parameter.
                if (_hourSlice) {
                    addParameter("hs");
                    addValue(_hourSlice);
                }

                // Cut last parameter if hash string is too long.
                if (_hashString.length >= 2000) {
                    _hashString = _hashString.slice(0, _hashString.lastIndexOf("&"));
                }

                // Set browser hash.
                location.hash = _hashString;

                // Fire an event on client state.
                _state.trigger("hashchange");
            }
        });

        /**
         * Initialization.
         */
        window.onhashchange = function () {
            var hashString = location.hash.substring(1);

            if (hashString !== _hashString) {
                self.parseState();
                self.update();

                // Fire an event on client state.
                _state.trigger("windowhashchange");
            }
        };
    };

    return FC;
}(FC || {}, window, jQuery));