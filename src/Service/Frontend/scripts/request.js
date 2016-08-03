// FetchClimate request definitions for HTML5 application

(function (FC, $, undefined) {

    // Temporal domain
    FC.TemporalDomain = function (years, yearCellMode, days, dayCellMode, hours, hourCellMode) {
        if (!$.isArray(years) || !$.isArray(days) || !$.isArray(hours))
            throw "Years, days and hours must be arrays";
        if (yearCellMode && years.length < 2)
            throw "At least two years must be specified in cell mode";
        if (dayCellMode && days.length < 2)
            throw "At least two days must be specified in cell mode";
        if (hourCellMode && hours.length < 2)
            throw "At least two hours must be specified in cell mode";
        this.years = years;
        this.yearCellMode = yearCellMode;
        this.days = days;
        this.dayCellMode = dayCellMode;
        this.hours = hours;
        this.hourCellMode = hourCellMode;
    };

    FC.TemporalDomain.prototype.isEqual = function (td) {
        if (this.yearCellMode !== td.yearCellMode ||
            this.dayCellMode !== td.dayCellMode ||
            this.hourCellMode !== td.hourCellMode) {
            return false;
        }

        if (!$.isArray(td.years) || !$.isArray(td.days) || !$.isArray(td.hours)) {
            return false;
        }

        if (this.years.length !== td.years.length ||
            this.days.length !== td.days.length ||
            this.hours.length !== td.hours.length) {
            return false;
        }

        for (var i = 0; i < this.years.length; i++) {
            if (this.years[i] !== td.years[i]) {
                return false;
            }
        }

        for (var i = 0; i < this.days.length; i++) {
            if (this.days[i] !== td.days[i]) {
                return false;
            }
        }

        for (var i = 0; i < this.hours.length; i++) {
            if (this.hours[i] !== td.hours[i]) {
                return false;
            }
        }

        return true;
    };

    FC.TemporalDomain.prototype.fillFetchRequest = function (request) {
        request.Domain = request.Domain || {};
        request.Domain.TimeRegion = {
            Years: this.years,
            Days: this.days,
            Hours: this.hours,
            IsIntervalsGridYears: this.yearCellMode,
            IsIntervalsGridDays: this.dayCellMode,
            IsIntervalsGridHours: this.hourCellMode
        };
    };

    FC.TemporalDomain.prototype.yearAxisLength = function () {
        return this.yearCellMode ? (this.years.length - 1) : this.years.length;
    };

    FC.TemporalDomain.prototype.dayAxisLength = function () {
        return this.dayCellMode ? (this.days.length - 1) : this.days.length;
    };

    FC.TemporalDomain.prototype.hourAxisLength = function () {
        return this.hourCellMode ? (this.hours.length - 1) : this.hours.length;
    };

    // ScatteredPoints spatial domain.
    // ScatteredPoints are constructed from two Numeric arrays of similar length or one array of GeoPoints
    FC.ScatteredPoints = function (a, b) {
        this.spatialRegionType = "Points";
        if (!b) {
            if (!$.isArray(a))
                throw "Argument must be an array";
            this.lats = [];
            this.lons = [];
            for (var i = 0; i < a.length; i++) {
                this.lats.push(a[i].lat);
                this.lons.push(a[i].lon);
            }
        } else {
            if (!$.isArray(a) || !$.isArray(b) || a.length != b.length)
                throw "Lats and lons must be arrays of same length";
            this.lats = a;
            this.lons = b;
        }
    };

    FC.ScatteredPoints.prototype.fillFetchRequest = function (request) {
        request.Domain = request.Domain || {};
        request.Domain.SpatialRegionType = "Points";
        request.Domain.Lats = this.lats;
        request.Domain.Lons = this.lons;
        request.Domain.Lats2 = null;
        request.Domain.Lons2 = null;
    };

    // Grid spatial domain base class
    FC.Grid = function (regionType, a, b, c, d, e, f) {
        this.spatialRegionType = regionType;
        if ($.isArray(a) && $.isArray(b) && !c && !d && !e && !f) {
            this.lats = a;
            this.lons = b;
        } else {
            this.lats = [];
            for (var i = 0; i < c; i++)
                this.lats[i] = a + (b - a) * i / (c - 1);
            this.lons = [];
            for (var i = 0; i < f; i++)
                this.lons[i] = d + (e - d) * i / (f - 1);
        }
    };

    FC.Grid.prototype.fillFetchRequest = function (request) {
        request.Domain = request.Domain || {};
        request.Domain.SpatialRegionType = this.spatialRegionType;
        request.Domain.Lats = this.lats;
        request.Domain.Lons = this.lons;
        request.Domain.Lats2 = null;
        request.Domain.Lons2 = null;
    };

    // CellGrid spatial domain
    // CellGrid is constructed either from arrays of lats and lons or
    // from six arguments: latmin, latmax, latcount, lonmin, lonmax, loncount
    FC.CellGrid = function (a, b, c, d, e, f) {
        FC.Grid.call(this, "CellGrid", a, b, c, d, e, f);
    };

    FC.CellGrid.prototype.fillFetchRequest = FC.Grid.prototype.fillFetchRequest;

    // PointGrid spatial domain
    // PointGrid is constructed either from arrays of lats and lons or
    // from six arguments: latmin, latmax, latcount, lonmin, lonmax, loncount
    FC.PointGrid = function (a, b, c, d, e, f) {
        FC.Grid.call(this, "PointGrid", a, b, c, d, e, f);
    };

    FC.PointGrid.prototype.fillFetchRequest = FC.Grid.prototype.fillFetchRequest;

    FC.CellGrid.prototype.fillFetchRequest = function (request) {
        request.Domain = request.Domain || {};
        request.Domain.SpatialRegionType = "CellGrid";
        request.Domain.Lats = this.lats;
        request.Domain.Lons = this.lons;
        request.Domain.Lats2 = null;
        request.Domain.Lons2 = null;
    };

    // ScattetedCells spatial domain
    // ScatteredCells are constructed from four arguments: latmin, lonmin, latmax, lonmax
    FC.ScatteredCells = function (a, b, c, d) {
        if (!$.isArray(latmin) || !$.isArray(latmax) || !$.isArray(lonmin) || !$.isArray(lonmax)) 
            throw "All arguments must be arrays";
        if(latmin.length != lonmin.length || lonmin.length != lonmax.length || lonmax.length != latmax.length)
            throw "All arguments must be arrays of the same length";
        this.spatialRegionType = "Cells";
        this.lats = latmin;
        this.lats2 = latmax;
        this.lons = lonmin;
        this.lons2 = lonmax;
    };

    FC.ScatteredCells.prototype.fillFetchRequest = function (request) {
        request.Domain = request.Domain || {};
        request.Domain.SpatialRegionType = "Cells";
        request.Domain.Lats = this.lats;
        request.Domain.Lons = this.lons;
        request.Domain.Lats2 = this.lats2;
        request.Domain.Lons2 = this.lons2;
    };

    // Response to a request. Normally, you don't need to construct these objects manually
    FC.Response = function (request, resultUri) {
        this.rq = request;
        this.uri = resultUri;

        Object.defineProperty(this, "request", {
            get: function () { return this.rq; },
            configurable: false
        });

        Object.defineProperty(this, "resultUri", {
            get: function () { return this.uri; },
            configurable: false
        });
    };

    function getRejectReason(request, textStatus, errorThrown) {
        if (errorThrown)
            return errorThrown;
            // As per jQuery docs: http://api.jquery.com/jQuery.ajax/
        else if (!textStatus || textStatus == "timeout" || textStatus == "abort" || textStatus == "error" || textStatus == "parsererror")
            return "timeout (" + request.timeout / 1000 + " sec.)";
        else
            return textStatus;
    }

    function getNowString() {
        var d = new Date();
        return d.toUTCString() + " +" + d.getMilliseconds() + "ms";
    }

    FC.Response.prototype.getSchema = function () {
        var r = new jQuery.Deferred();
        $.ajax({
            url: this.rq.serviceUrl + "/jsproxy/schema?uri=" + encodeURIComponent(this.uri),
            type: "GET",
            dataType: "json",
            timeout: this.rq.timeout,
            success: function (result) {
                r.resolve(result);
            },
            error: function (jqXHR, textStatus, errorThrown) {
                r.reject(getRejectReason(this.rq, textStatus, errorThrown));
            }
        });
        return r.promise();
    };

    function FilterAndBoundFloatData(a) {
        var min = Number.MAX_VALUE;
        var max = -Number.MAX_VALUE;
        if ($.isArray(a)) {
            var a2 = a[0];
            if ($.isArray(a2))
                for (var i = 0; i < a.length; i++) {
                    var d = a[i];
                    FilterAndBoundFloatData(d);
                    if (min > d.max)
                        min = d.max;
                    else if (min > d.min)
                        min = d.min;
                    if (max < d.min)
                        max = d.min;
                    else if (max < d.max)
                        max = d.max;
                }
            else
                for (var i = 0; i < a.length; i++) {
                    var x = parseFloat(a[i]);
                    if (min > x)
                        min = x;
                    if (max < x)
                        max = x;
                    a[i] = x;
                }
        } else {
            a = parseFloat(a);
            if (min > a)
                min = a;
            if (max < a)
                max = a;
        }
        if (min == Number.MAX_VALUE)
            min = max = Number.NaN;
        a.min = min;
        a.max = max;
        return a;
    }

    FC.Response.prototype.getData = function (names) {
        if (!names) {
            if (this.request.dataSources.length === 1) {
                names = ["values", "sd"];
            }
            else {
                names = ["values", "provenance", "sd"];
            }
        }

        var r = new jQuery.Deferred();
        console.log(getNowString() + ": Requesting " + names + " of " + this.uri);
        var vars = "";
        for (var i = 0; i < names.length; i++) {
            if(vars.length > 0)
                vars = vars + ",";
            vars += names[i];
        }
        $.ajax({
            url: this.rq.serviceUrl + "/jsproxy/data?uri=" + encodeURIComponent(this.uri) + "&variables=" + encodeURIComponent(vars),
            type: "GET",
            dataType: "json",
            timeout: this.timeout,
            success: function (result) {
                console.log(getNowString() + ": Received data for " + names);
                for (var i = 0; i < names.length; i++)
                    FilterAndBoundFloatData(result[names[i]]);
                r.resolve(result);
            },
            error: function (jqXHR, textStatus, errorThrown) {
                var reason = getRejectReason(this.rq, textStatus, errorThrown);
                console.log(getNowString() + ": Error getting " + names + ": " + reason);
                r.reject(reason);
            }
        });
        return r.promise();
    };

    FC.Response.prototype.getValues = function () {
        return this.getData("values");
    };

    FC.Response.prototype.getUncertainty = function () {
        return this.getData("sd");
    };

    FC.Response.prototype.getProvenance = function () {
        return this.getData("provenance");
    };

    // Request represents single FetchClimate request
    FC.Request = function (options) {

        var status = "new"; // Status is "new" | "pending" | "calculating" | "receiving" | "completed" | "failed"
        var statusData = null; // Status data is null | position in queue (may be NaN) | progress 0..100 | uri of blob | error message
        var _dataSources = {};

        Object.defineProperty(this, "status", {
            get: function () { return status; },
            set: function (value) { status = value; },
            configurable: false
        });

        Object.defineProperty(this, "statusData", {
            set: function (value) { statusData = value; },
            configurable: false
        });

        Object.defineProperty(this, "positionInQueue", {
            get: function () { return (status != "pending")? NaN :statusData; },
            configurable: false
        });

        Object.defineProperty(this, "percentCompleted", {
            get: function () {
                if (status === "calculating")
                    return statusData;
                else if (status === "receiving" || status === "completed" || status === "failed")
                    return 100;
                else
                    return 0;
            },
            configurable: false
        });

        Object.defineProperty(this, "errorMessage", {
            get: function () { return (status != "failed") ? "" : statusData; },
            configurable: false
        });

        Object.defineProperty(this, "resultUrl", {
            get: function () { return (status != "completed") ? null : statusData; },
            configurable: false
        });

        Object.defineProperty(this, "dataSources", {
            get: function () {
                return _dataSources;
            }
        });

        this.timeout = options.timeout ? options.timeout : 180000; // Default ajax timeout is 3 minutes
        this.pollInterval = options.pollingInterval ? options.pollingInterval : 10000; // Default polling interval is 10 seconds
        this.serviceUrl = options.serviceUrl ? options.serviceUrl : "";
        _dataSources = options.dataSources || [];
        
        var requestJSON;
        if (options.rawJSON)
            requestJSON = options.rawJSON;
        else {

            this.spatial = options.spatial;
            this.temporal = options.temporal;
            this.variable = options.variable;

            requestJSON = {
                EnvironmentVariableName: this.variable,
                Domain: {
                    Mask: null
                },
                ParticularDataSources: options.dataSources ? options.dataSources : {},
                ReproducibilityTimestamp: options.timestamp ? options.timestamp : Date.UTC(9999, 12, 31, 23, 59, 59, 999)
            };

            this.spatial.fillFetchRequest(requestJSON);
            this.temporal.fillFetchRequest(requestJSON);
        }

        var self = this;

        var performResult = null;
        var statusCallback = null;

        var failRequest = function (message)
        {
            status = "failed";
            statusData = message;
            if (performResult && performResult.reject) performResult.reject(message);
            performResult = statusCallback = null;
        }

        var onAjaxSuccess = function (answer) {
            // Expected values on stat5: 'pendi' | 'progr' | 'compl' | 'fault'
            var stat5 = answer.substring(0, Math.min(answer.length, 5));

            console.log(getNowString() + ": Status received " + answer);

            if (stat5 === "pendi" || stat5 === "progr") {
                // 'pending', 'progress' responses contain hash.
                var hashIdx = answer.indexOf("hash=");

                if (hashIdx == -1) 
                    failRequest("No hash found in response: " + answer);
                else {
                    self.hash = answer.substring(hashIdx + 5).trim();
                }

                if (stat5 === "pendi") {
                    status = "pending";

                    // Get queue position (positive number).
                    statusData = parseInt(answer.match(/pending=(\d+);/)[1], 10);

                    if (statusCallback) {
                        statusCallback(self);
                    }

                    setTimeout(doStatusCheck, self.pollInterval);
                }
                else if (stat5 === "progr") {
                    status = "calculating";

                    // Get progress value.
                    statusData = parseInt(answer.match(/progress=(\d+)%;/)[1], 10);

                    if (statusCallback) {
                        statusCallback(self);
                    }

                    setTimeout(doStatusCheck, self.pollInterval);
                }
            }
            else if (stat5 === "compl") {
                status = "receiving";
                statusData = answer.substring(10);

                // Decode the completed string of the form:
                // completed=msds:ab?Blob=<server>/<database>/<hash> [ &Container=... ]
                var a = answer;
                var hashIdx = a.indexOf("Blob=");
                if (hashIdx !== -1) {
                    a = a.substring(hashIdx + 5);
                    var idx = a.indexOf("&");
                    if (idx !== -1) { a = a.substring(0, idx); }
                    var idx = a.lastIndexOf("/");
                    if (idx !== -1) { a = a.substring(idx+1); }
                    self.hash = a.trim();
                }

                if (statusCallback) {
                    statusCallback(self);
                }

                performResult.resolve(new FC.Response(self, statusData));
                performResult = statusCallback = null;
            }
            else if (stat5 === "fault") {
                // 'fault' responses contain hash.
                var hashIdx = answer.indexOf("hash ");

                if (hashIdx == -1) 
                    failRequest("No hash found in response: " + answer);
                else {
                    self.hash = answer.substring(hashIdx + 5).trim();
                }

                failRequest(answer);
            }
            else {
                failRequest("Unexpected response: " + answer);
            }
        };

        var onAjaxError = function (jqXHR, textStatus, errorThrown) {
            var reason = getRejectReason(self, textStatus, errorThrown);
            console.log(getNowString() + ": Request failed: " + reason);
            failRequest(reason);
        };

        var doPost = function () {
            console.log(getNowString() + ": Invoking compute for variable " + requestJSON.EnvironmentVariableName);
            status = "pending";
            statusData = NaN; // Position in queue is uknown
            $.ajax({
                url: self.serviceUrl + "/api/compute",
                type: "POST",
                data: JSON.stringify(requestJSON),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                timeout: self.timeout,
                success: onAjaxSuccess,
                error: onAjaxError
            });
        };

        var doStatusCheck = function () {
            console.log(getNowString() + ": Getting state for " + self.hash);
            $.ajax({
                url: self.serviceUrl + "/api/status?hash=" + self.hash,
                type: "GET",
                dataType: "json",
                timeout: self.timeout,
                success: onAjaxSuccess,
                error: onAjaxError
            });
        };

        this.perform = function (callback) {
            if (!performResult) {
                performResult = new jQuery.Deferred();
                statusCallback = callback;
                doPost();
            }
            return performResult.promise();
        };
    };
    
    // Appends list of data sources to each variable
    function appendDataSourceInfo(config) {
        for (var i = 0; i < config.EnvironmentalVariables.length; i++) {
            var v = config.EnvironmentalVariables[i];
            v.DataSources = [];
            for (var j = 0; j < config.DataSources.length; j++) {
                var d = config.DataSources[j];
                var found = false;
                for (var k = 0; k < d.ProvidedVariables.length && !found; k++)
                    found = (d.ProvidedVariables[k] == v.Name);
                if (found)
                    v.DataSources.push(d);
            }
        }
        return config;
    }

    function parseCategories(config) {
        var cats = [];
        for (var i = 0; i < config.EnvironmentalVariables.length; i++) {
            var v = config.EnvironmentalVariables[i];
            var p = FC.getCategoriesFromDescription(v.Description);
            v.Description = p.description;
            v.Categories = p.categories;
            for (var j = 0; j < v.Categories.length; j++) {
                var unique = true;
                for (var k = 0; k < cats.length && unique; k++)
                    unique = (cats[k] != v.Categories[j]);
                if (unique)
                    cats.push(v.Categories[j]);
            }
        }
        config.Categories = cats;
        return config;
    }

    FC.getConfigurationTimeout = 180000; // 3 minute timeout for configuration request

    // Returns FetchClimate configuration for given timestamp or latest configuration
    // if timestamp is not specified
    FC.getConfiguration = function (options) {
        options = options || {};
        var r = new jQuery.Deferred();
        $.ajax({
            url: (options.serviceUrl ? options.serviceUrl : "") +
                (options.timestamp ? "/api/configuration?timestamp=" + options.timestamp : "/api/configuration"),
            type: "GET",
            dataType: "json",
            timeout: FC.getConfigurationTimeout,
            success: function (result) {
                r.resolve(parseCategories(appendDataSourceInfo(result)));
            },
            error: function (jqXHR, textStatus, errorThrown) {
                r.reject(getRejectReason(self, textStatus, errorThrown));
            }
        });
        return r.promise();
    };

    FC.getCategoriesFromDescription = function (description) {
        // Work correctly with null
        if (!description)
            return {
                description: "",
                categories: []
            };

        // Split description and categories
        var idx = description.lastIndexOf("Category:");
        var cstr = "";
        if (idx != -1) {
            cstr = description.substring(idx + 9).trim();
            description = description.substring(0, idx).trim();
        } else {
            idx = description.lastIndexOf("Categories:");
            if (idx != -1) {
                cstr = description.substring(idx + 11).trim();
                description = description.substring(0, idx).trim();
            } else
                return {
                    description: description,
                    categories: []
                };
        }
        // Remove trailing ';' if exists
        if (description.charAt(description.length - 1) == ';')
            description = description.substring(0, description.length - 1);

        // Remove trailing '.' if exists.
        if (cstr.charAt(cstr.length - 1) == '.')
            cstr = cstr.substring(0, cstr.length - 1);

        // Split into parts by commas
        var cats = cstr.split(",");
        for (var i = 0; i < cats.length;) {
            cats[i] = cats[i].trim();
            if (cats[i] === "")
                cats.splice(i, 1);
            else
                i++;
        }

        return {
            description: description,
            categories: cats
        };
    };

})(window.FC = window.FC || {}, jQuery);