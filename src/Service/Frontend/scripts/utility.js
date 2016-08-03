// Utility subroutines and classes for FetchClimate HTML5 client

(function (FC, $, undefined) {

    FC.GeoEps = 1e-4; // Possible rounding for geographical coordinates

    FC.geoRound = function (v) {
        return Math.round(v * 10000) / 10000;
    };

    FC.GeoPoint = function (lat, lon, name, id) {
        this.lat = parseFloat(lat);
        this.lon = parseFloat(lon);
        this.name = name;
        this.id = id;
    };

    FC.GeoPoint.prototype.isEqual = function (p2, eps) {
        if (eps === undefined)
            eps = FC.GeoEps;
        return Math.abs(this.lat - p2.lat) < eps &&
            Math.abs(this.lon - p2.lon) < eps;
    };

    FC.GeoPoint.prototype.toString = function () {
        return [this.lat, this.lon, encodeURIComponent(this.name)].join(",");
    };

    FC.GeoRect = function (latmin, latmax, lonmin, lonmax) {
        this.latmin = latmin;
        this.latmax = latmax;
        this.lonmin = lonmin;
        this.lonmax = lonmax;
    };

    FC.GeoRect.prototype.isEqual = function (r2, eps) {
        if (eps === undefined)
            eps = FC.GeoEps;
        return Math.abs(this.latmin - r2.latmin) < eps &&
            Math.abs(this.latmax - r2.latmax) < eps &&
            Math.abs(this.lonmin - r2.lonmin) < eps &&
            Math.abs(this.lonmax - r2.lonmax) < eps;
    };

    FC.GeoRect.toPlotRect = function (gr) {
        var ymin = D3.mercatorTransform.dataToPlot(gr.latmin);
        return {
            x: gr.lonmin,
            y: ymin,
            width: gr.lonmax - gr.lonmin,
            height: D3.mercatorTransform.dataToPlot(gr.latmax) - ymin
        };
    };

    FC.GeoRect.prototype.toShortString = function () {
        return FC.geoRound(this.latmin) + "," +
            FC.geoRound(this.latmax) + "," +
            FC.geoRound(this.lonmin) + "," +
            FC.geoRound(this.lonmax);
    };

    FC.GeoRect.fromPlotRect = function (vr) {
        var latmin = D3.mercatorTransform.plotToData(vr.y);
        return new FC.GeoRect(latmin, D3.mercatorTransform.plotToData(vr.y + vr.height), vr.x, vr.x + vr.width);
    };

    FC.GeoRect.fromShortString = function (s) {
        var coords = s.split(",");
        return new FC.GeoRect(
            parseFloat(coords[0]),
            parseFloat(coords[1]),
            parseFloat(coords[2]),
            parseFloat(coords[3]));
    };

    function getUniformGrid(min, max, count) {
        var result = new Array(count);
        for (var i = 0; i < count; i++)
            result[i] = min + (max - min) * i / (count - 1);
        return result;
    }

    // Spatial rectangular uniform grid
    FC.GeoGrid = function (latmin, latmax, latcount, lonmin, lonmax, loncount, name, id) {
        if (latmin >= latmax)
            throw "Error constructing GeoGrid: latmin >= latmax";
        if (lonmin >= lonmax)
            throw "Error constructing GeoGrid: lonmin >= lonmax";
        if (latcount < 2)
            throw "Error constructing GeoGrid: at least two points along latitude are required";
        if (loncount < 2)
            throw "Error constructing GeoGrid: at least two points along longitude are required";
        if (!name)
            throw "Error constructing GeoGrid: name can't be empty";
        this.latmin = parseFloat(latmin);
        this.latmax = parseFloat(latmax);
        this.latcount = parseInt(latcount);
        this.lonmin = parseFloat(lonmin);
        this.lonmax = parseFloat(lonmax);
        this.loncount = parseInt(loncount);
        this.name = name;
        this.id = id;
    };

    FC.GeoGrid.prototype.isEqual = function (g2, eps) {
        if (eps === undefined)
            eps = FC.GeoEps;
        return Math.abs(this.latmin - g2.latmin) < eps &&
            Math.abs(this.latmax - g2.latmax) < eps &&
            Math.abs(this.latcount - g2.latcount) < eps &&
            Math.abs(this.lonmin - g2.lonmin) < eps &&
            Math.abs(this.lonmax - g2.lonmax) < eps &&
            Math.abs(this.loncount - g2.loncount) < eps;
    };

    FC.GeoGrid.prototype.getLatGrid = function () {
        return getUniformGrid(this.latmin, this.latmax, this.latcount);
    };

    FC.GeoGrid.prototype.getLonGrid = function () {
        return getUniformGrid(this.lonmin, this.lonmax, this.loncount);
    };

    FC.GeoGrid.prototype.toString = function () {
        return [this.latmin, this.latmax, this.latcount, this.lonmin, this.lonmax, this.loncount, encodeURIComponent(this.name)].join(",");
    };

    FC.parseMatlabSequence = function (s) {
        if (s.indexOf(":") >= 0) {
            var seq = s.split(":");
            if (seq.length == 2) {
                var from = parseFloat(seq[0]);
                var to = parseFloat(seq[1]);
                if (from > to)
                    throw "End of sequence should be less than start";
                var result = [];
                while (from <= to) {
                    result.push(from);
                    from++;
                }
                return result;
            } else if (seq.length == 3) {
                var from = parseFloat(seq[0]);
                var to = parseFloat(seq[2]);
                var step = parseFloat(seq[1]);
                if (from > to)
                    throw "End of sequence should be less than start";
                if (step <= 0)
                    throw "Step should be positive";
                var result = [];
                while (from <= to) {
                    result.push(from);
                    from += step;
                }
                return result;
            } else if (seq.length > 3)
                throw "Too many points in matlab notation";
        } else if (s.indexOf(",") >= 0) {
            var seq = s.split(",");
            var result = [];
            for (var i = 0; i < seq.length; i++)
                result.push(parseFloat(seq[i]));
            return result;
        } else
            return [parseFloat(s)];
    };

    FC.getMatlabSequence = function (a) {
        if (a.length == 1)
            return a[0] + "";
        else if (a.length == 2)
            return a[0] + "," + a[1];
        else {
            var step = a[1] - a[0];
            var isConstantStep = true;
            for (var i = 1; i < a.length - 2; i++)
                if (Math.abs(a[i + 1] - a[i] - step) > 1e-10) {
                    isConstantStep = false;
                    break;
                }
            if (isConstantStep)
                return a[0] + ":" + step + ":" + a[a.length - 1];
            else {
                var result = "";
                for (var i = 0; i < a.length; i++) {
                    if (result.length > 0)
                        result = result + ",";
                    result = result + a[i];
                }
                return result;
            }
        }
    };

    FC.TemporalDomainBuilder = function () {
        var years, days, hours;
        var isYearPoints, isDayPoints, isHourPoints;

        var self = this;

        function parseYears(s) {
            if (years)
                throw "Year axis is already defined";
            var yp = FC.parseMatlabSequence(s);
            if (yp.length == 1 && !isYearPoints)
                throw "At least two points must be defined for year cells";
            years = yp;
        }

        function parseDays(s) {
            if (days)
                throw "Days axis is already defined";
            var dp = FC.parseMatlabSequence(s);
            if (dp.length == 1 && !isDayPoints)
                throw "At least two points must be defined for day cells";
            days = dp;
        }

        function parseHours(s) {
            if (hours)
                throw "Hour axis is already defined";
            var hp = FC.parseMatlabSequence(s);
            if (hp.length == 1 && !isHourPoints)
                throw "At least two points must be defined for hour cells";
            hours = hp;
        }

        this.parseYearCells = function (s) {
            isYearPoints = false;
            parseYears(s);
        };

        this.parseDayCells = function (s) {
            isDayPoints = false;
            parseDays(s);
        };

        this.parseHourCells = function (s) {
            isHourPoints = false;
            parseHours(s);
        };

        this.parseYearPoints = function (s) {
            isYearPoints = true;
            parseYears(s);
        };

        this.parseDayPoints = function (s) {
            isDayPoints = true;
            parseDays(s);
        };

        this.parseHourPoints = function (s) {
            isHourPoints = true;
            parseHours(s);
        };

        this.getTemporalDomain = function () {
            if (!years || !days || !hours)
                return null;
            return new FC.TemporalDomain(years, !isYearPoints,
                days, !isDayPoints,
                hours, !isHourPoints);
        };

        this.getTemporalDomainString = function (td) {
            return (td.yearCellMode ? "yc=" : "y=") + FC.getMatlabSequence(td.years) + "&" +
                (td.dayCellMode ? "dc=" : "d=") + FC.getMatlabSequence(td.days) + "&" +
                (td.hourCellMode ? "hc=" : "h=") + FC.getMatlabSequence(td.hours);
        };
    };

    FC.isMonthlyCellAxis = function (days) {
        return (days.length == 13 &&
            days[0] == 1 && days[1] == 32 && days[2] == 60 && days[3] == 91 &&
            days[4] == 121 && days[5] == 152 && days[6] == 182 && days[7] == 213 &&
            days[8] == 244 && days[9] == 274 && days[10] == 305 && days[11] == 335 && days[12] == 366);
    };

    FC.getTimeAxisLabels = function (td, ta) {
        /// <summary>Returns array of label for specified time axis ("hours", "days", "years") of given temporal domain. Returns empty array in case of error.</summary>

        function hourToString(h) {
            if (h === 0 || h === 24)
                return "12pm";
            else if (h < 12)
                return h + "am";
            else if (h == 12)
                return "12am";
            else
                return (h - 12) + "pm";
        }

        // TODO: Use JS to generate localized names
        var monthLabels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

        var result = [];
        if (ta == "hours") {
            if (td.hourCellMode) {
                for (var i = 0; i < td.hours.length - 1; i++) {
                    var s = td.hours[i];
                    var e = td.hours[i + 1] - 1;
                    result.push((s < e) ? (hourToString(s) + ".." + hourToString(e)) : (hourToString(s) + ""));
                }
            } else {
                for (var i = 0; i < td.hours.length; i++)
                    result.push(hourToString(td.hours[i]));
            }
        } else if (ta == "days") {
            if (td.dayCellMode) {
                if (FC.isMonthlyCellAxis(td.days))
                    return monthLabels;
                else
                    for (var i = 0; i < td.days.length - 1; i++) {
                        var s = td.days[i];
                        var e = td.days[i + 1] - 1;
                        result.push((s < e) ? (s + ".." + e) : (s + ""));
                    }
            } else {
                for (var i = 0; i < td.days.length; i++) 
                    result.push(td.days[i]);
            }
        } else if (ta == "years") {
            if (td.yearCellMode) {
                for (var i = 0; i < td.years.length - 1; i++) {
                    var s = td.years[i];
                    var e = td.years[i + 1] - 1;
                    result.push((s < e) ? (s + ".." + e) : (s + ""));
                }
            } else {
                for (var i = 0; i < td.years.length; i++)
                    result.push(td.years[i]);
            }
        } 
        return result;
    };

    FC.appendUniqueProvenanceIDs = function (a, ids) {
        if ($.isArray(a[0])) {
            for (var i = 0; i < a.length; i++)
                FC.appendUniqueProvenanceIDs(a[i], ids);
        } else {
            for (var i = 0; i < a.length; i++)
                if (a[i] != 65535 && ids.indexOf(a[i]) == -1)
                    ids.push(a[i]);
        }
    };

    /**
     * Check if given year is leap.
     */
    FC.isLeapYear = function isLeapYear (year) {
        return year > 1582 &&
            (0 === year % 400 || (0 === year % 4 && 0 !== year % 100));
    };

    /**
     * Returns string with number v rounded to n digits after decimal point. 
     * Returns v is v is not a number.
     */
    FC.roundTo = function roundTo(v, n) {
        if (isFinite(String(v))) {
            var scale = Math.pow(10, n);
            return (Math.round(v * scale) / scale).toFixed(n);
        } else {
            return v;
        }
    };

    FC.openUrlInNewTab = function (url) {
        $("<a></a>").on("click", function (event) {
                window.open(url, "_blank");
            })
            .trigger("click")
            .off("click")
            .remove();
    };

})(window.FC = window.FC || {}, jQuery);