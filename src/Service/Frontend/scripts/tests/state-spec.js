/// <reference path="spec-helper.js"/>
/// <reference path="..\external\jquery-2.0.3.min.js"/>
/// <reference path="..\external\rx.js"/>
/// <reference path="..\external\rx.binding.js"/>
/// <reference path="..\external\rx.jquery.js"/>
/// <reference path="..\external\d3.js"/>
/// <reference path="..\settings.js"/>
/// <reference path="..\utility.js"/>
/// <reference path="..\request.js"/>
/// <reference path="..\client-state.js"/>
/// <reference path="..\map.js"/>

describe("FC.ClientState", function () {
    var state;

    function setHash(value) {
        location.hash = value;
        window.onhashchange();
    }

    beforeEach(function () {
        FC.state = state = new FC.ClientState({
            url: "http://fetchclimate2staging5.cloudapp.net",
            getConfiguration: FC.getConfiguration,
            performRequest: function (spatial, temporal) {
                var request = new FC.Request({
                    spatial: spatial,
                    temporal: temporal
                });
                return request.perform();
            }
        });
        state.hash.parseState();
        state.hash.update();
    });

    afterEach(function() {
        if (state) state.reset();
        location.hash = "";
    });

    describe("FC.Hash update() method", function () {
        it("should cut the last parameter from the hash string because it is too long", function () {
            var hashLengthBefore,
                hashLengthAfter;

            state.setActivePage("geography");
            hashLengthBefore = state.hash.hashString.length;

            for (var i = 0; i < 200; ++i) {
                state.toggleVariable("var" + i, [101, 102, 103], true);
            }

            state.hash.update();
            hashLengthAfter = state.hash.hashString.length;
            expect(hashLengthBefore).toEqual(hashLengthAfter);
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });

        using("valid pages", ["geography", "layers", "time", "results", "export", "about"], function(value) {
            it("should set active page to the hash", function () {
                state.setActivePage(value);
                expect(state.hash.hashString).toEqual("page=" + value + "&dm=values&t=years");
                expect("#" + state.hash.hashString).toEqual(location.hash);
            });
        });

        it("should set geography active page to the hash if page is invalid", function () {
            state.setActivePage("invalid-page");
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years");
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });

        it("should add variable and its data sources to the hash", function () {
            state.toggleVariable("var", [101, 102, 103]);
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&v=var(101,102,103)");
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });

        it("should add variable to and remove it from the hash (toggling)", function () {
            state.toggleVariable("var");
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&v=var");
            expect("#" + state.hash.hashString).toEqual(location.hash);
            state.toggleVariable("var");
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years");
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });

        it("should add data source to and remove it from a variable in the hash (toggling)", function () {
            state.toggleVariable("var", [101]);
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&v=var(101)");
            expect("#" + state.hash.hashString).toEqual(location.hash);
            state.toggleDataSource("var", 102);
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&v=var(101,102)");
            expect("#" + state.hash.hashString).toEqual(location.hash);
            state.toggleDataSource("var", 102);
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&v=var(101)");
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });

        it("should add temporal domain parameters (cells) to the hash", function () {
            var tdb = new FC.TemporalDomainBuilder();
            tdb.parseYearCells("1990,2000");
            tdb.parseDayCells("1,32,60,91,121,152,182,213,244,274,305,335,366");
            tdb.parseHourCells("0,24");
            state.setTemporal(tdb.getTemporalDomain());
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&yc=1990,2000&dc=1,32,60,91,121,152,182,213,244,274,305,335,366&hc=0,24");
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });

        it("should add temporal domain parameters (ranges) to the hash", function () {
            var tdb = new FC.TemporalDomainBuilder();
            tdb.parseYearPoints("1990:1:1999");
            tdb.parseDayPoints("1:5:365"); // 365 is not included, the last day is 361.
            tdb.parseHourPoints("0:1:23");
            state.setTemporal(tdb.getTemporalDomain());
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&y=1990:1:1999&d=1:5:361&h=0:1:23");
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });

        it("should add temporal domain parameters (points) to the hash", function () {
            var tdb = new FC.TemporalDomainBuilder();
            tdb.parseYearPoints("1990,2000");
            tdb.parseDayPoints("1,32,60,91,121,152,182,213,244,274,305,335,366");
            tdb.parseHourPoints("0,24");
            state.setTemporal(tdb.getTemporalDomain());
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&y=1990,2000&d=1,32,60,91,121,152,182,213,244,274,305,335,366&h=0,24");
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });

        it("should add grids parameter to the hash", function () {
            var grid = new FC.GeoGrid(1, 2, 2, 1, 2, 2, "grid");
            state.addGrid(grid);
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&g=1,2,2,1,2,2,grid");
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });

        it("should encode name of grid added to grids parameter to the hash", function () {
            var name = "grid to be, encoded&?=";
            var encodedName = encodeURIComponent(name);
            var grid = new FC.GeoGrid(1, 2, 2, 1, 2, 2, name);
            state.addGrid(grid);
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&g=1,2,2,1,2,2," + encodedName);
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });

        it("should add points parameter to the hash", function () {
            var point = new FC.GeoPoint(1, 2, "point");
            state.addPoint(point);
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&p=1,2,point");
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });

        it("should encode name of point added to points parameter to the hash", function () {
            var name = "point to be, encoded&?=";
            var encodedName = encodeURIComponent(name);
            var point = new FC.GeoPoint(1, 2, name);
            state.addPoint(point);
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&p=1,2," + encodedName);
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });
    });

    describe("FC.Hash parseState() method", function () {
        using("valid pages", ["geography", "layers", "time", "results", "export", "about"], function(value) {
            it("should parse active page from the hash", function () {
                setHash("page=" + value);
                expect(state.activePage).toEqual(value);
            });
        });

        it("should parse variables without data sources from the hash", function () {
            ["var1", "var2", "var3"].forEach(function (variableName) {
                expect(state.variables[variableName]).toBeUndefined();
            });

            setHash("v=var1,var2,var3");

            ["var1", "var2", "var3"].forEach(function (variableName) {
                expect(state.variables[variableName]).toBeDefined();
            });
        });

        it("should parse data sources for each variable from the hash", function () {
            setHash("v=var1,var2,var3");

            ["var1", "var2", "var3"].forEach(function (variableName) {
                expect(state.variables[variableName]).toBeDefined();
                expect(state.variables[variableName].dataSources).toEqual([]);
            });

            setHash("v=var1,var2(101),var3(102,103)");

            expect(state.variables.var1.dataSources).toEqual([]);
            expect(state.variables.var2.dataSources).toEqual([101]);
            expect(state.variables.var3.dataSources).toEqual([102, 103]);
        });

        it("should parse temporal domain parameters (cells) from the hash", function () {
            setHash("yc=1990,2000&dc=1,32,60,91,121,152,182,213,244,274,305,335,366&hc=0,24");
            expect(state.temporal).toBeDefined();
            expect(state.temporal.yearCellMode).toBe(true);
            expect(state.temporal.dayCellMode).toBe(true);
            expect(state.temporal.hourCellMode).toBe(true);
            expect(state.temporal.years).toEqual([1990, 2000]);
            expect(state.temporal.days).toEqual([1, 32, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366]);
            expect(state.temporal.hours).toEqual([0, 24]);
        });

        it("should parse temporal domain parameters (ranges) from the hash", function () {
            setHash("y=1990:1:1994&d=1:5:35&h=0:1:10");
            expect(state.temporal).toBeDefined();
            expect(state.temporal.yearCellMode).toBe(false);
            expect(state.temporal.dayCellMode).toBe(false);
            expect(state.temporal.hourCellMode).toBe(false);
            expect(state.temporal.years).toEqual([1990, 1991, 1992, 1993, 1994]);
            expect(state.temporal.days).toEqual([1, 6, 11, 16, 21, 26, 31]);
            expect(state.temporal.hours).toEqual([0, 1, 2, 3, 4, 5, 6, 7 , 8, 9, 10]);
        });

        it("should parse temporal domain parameters (points) from the hash", function () {
            setHash("y=1990,1995&d=1,100,200,300&h=1,10,20");
            expect(state.temporal).toBeDefined();
            expect(state.temporal.yearCellMode).toBe(false);
            expect(state.temporal.dayCellMode).toBe(false);
            expect(state.temporal.hourCellMode).toBe(false);
            expect(state.temporal.years).toEqual([1990, 1995]);
            expect(state.temporal.days).toEqual([1, 100, 200, 300]);
            expect(state.temporal.hours).toEqual([1, 10, 20]);
        });

        it("should parse grid in grids parameter from the hash", function () {
            setHash("g=1,2,2,1,2,2,grid");
            expect(state.grids[0]).toBeDefined();
            expect(state.grids[0].latmin).toEqual(1);
            expect(state.grids[0].latmax).toEqual(2);
            expect(state.grids[0].latcount).toEqual(2);
            expect(state.grids[0].lonmin).toEqual(1);
            expect(state.grids[0].lonmax).toEqual(2);
            expect(state.grids[0].loncount).toEqual(2);
            expect(state.grids[0].name).toEqual("grid");
        });

        it("should decode name of parsed grid in grids parameter from the hash", function () {
            var name = "grid to be, encoded&?=";
            var encodedName = encodeURIComponent(name);
            setHash("g=1,2,2,1,2,2," + encodedName);
            expect(state.grids[0].name).toEqual(name);
        });

        it("should parse point in points parameter from the hash", function () {
            setHash("p=1,2,point");
            expect(state.points[0]).toBeDefined();
            expect(state.points[0].lat).toEqual(1);
            expect(state.points[0].lon).toEqual(2);
            expect(state.points[0].name).toEqual("point");
        });

        it("should decode name of parsed point in points parameter from the hash", function () {
            var name = "point to be, encoded&?=";
            var encodedName = encodeURIComponent(name);
            setHash("p=1,2," + encodedName);
            console.log(state.hash.hashString);
            expect(state.points[0].name).toEqual(name);
        });

        it("should ignore invalid parameter in the hash", function () {
            setHash("v=var(101)&test=test");
            expect(state.variables["var"]).toBeDefined();
            expect(state.variables["var"].dataSources).toEqual([101]);
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&v=var(101)");
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });

        it("should ignore invalid variable in the hash", function () {
            state.config = {
                EnvironmentalVariables: [{
                    Name: "valid",
                    DataSources: [{
                        ID: 101
                    }]
                }]
            };

            setHash("v=valid,invalid");
            expect(state.variables.valid).toBeDefined();
            expect(state.variables.invalid).toBeUndefined();
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&v=valid");
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });

        it("should ignore invalid data sources for particular variable in the hash", function () {
            state.config = {
                EnvironmentalVariables: [{
                    Name: "var",
                    DataSources: [{
                        ID: 101
                    }]
                }]
            };

            setHash("v=var(101,102,103)");
            expect(state.variables["var"]).toBeDefined();
            expect(state.hash.hashString).toEqual("page=geography&dm=values&t=years&v=var(101)");
            expect("#" + state.hash.hashString).toEqual(location.hash);
        });
    });

    describe("FC.ClientState events", function () {
        it("fires an event callback after adding it to an event and triggering", function () {
            var callbackSpy = jasmine.createSpy("callbackSpy");
            state.on("test", callbackSpy);
            expect(callbackSpy).not.toHaveBeenCalled();
            state.trigger("test");
            expect(callbackSpy).toHaveBeenCalled();
        });

        it("doesn't fire particular event callback after removing it from an event and triggering", function () {
            var callbackSpy1 = jasmine.createSpy("callbackSpy1"),
                callbackSpy2 = jasmine.createSpy("callbackSpy2");
            state.on("test", callbackSpy1);
            state.on("test", callbackSpy2);
            state.off("test", callbackSpy2);
            state.trigger("test");
            expect(callbackSpy1).toHaveBeenCalled();
            expect(callbackSpy2).not.toHaveBeenCalled();
        });

        it("fires an event callback for each event after adding it to multiple events and triggering", function () {
            var callbackSpy = jasmine.createSpy("callbackSpy");
            state.on("test1 test2 test3", callbackSpy);

            ["test1", "test2", "test3"].forEach(function (eventName) {
                state.trigger(eventName);
                expect(callbackSpy).toHaveBeenCalled();
                callbackSpy.reset();
            });

            state.trigger("test1");
            state.trigger("test2");
            state.trigger("test3");

            expect(callbackSpy.callCount).toEqual(3);
        });

        it("fires an event callback with given arguments for the trigger() method", function () {
            var callbackSpy = jasmine.createSpy("callbackSpy");
            state.on("test", callbackSpy);
            state.trigger("test", 123, "foo", { bar: true }, [1, 2, 3]);
            expect(callbackSpy).toHaveBeenCalledWith(123, "foo", { bar: true }, [1, 2, 3]);
        });

        it("fires a 'hashchange' and 'windowhashchange' events on hash change in address bar", function () {
            spyOn(state, "trigger");
            setHash("test");

            expect(state.trigger.callCount).toEqual(2);
            expect(state.trigger.argsForCall).toEqual([["hashchange"], ["windowhashchange"]]);
        });

        it("fires a 'hashchange' event on state change", function () {
            spyOn(state, "trigger");

            state.setActivePage("test");
            expect(state.trigger).toHaveBeenCalledWith("hashchange");
            state.trigger.reset();

            state.toggleVariable("var", []);
            expect(state.trigger).toHaveBeenCalledWith("hashchange");
            state.trigger.reset();

            state.toggleDataSource("var", 101);
            expect(state.trigger).toHaveBeenCalledWith("hashchange");
            state.trigger.reset();

            state.setGrids([]);
            expect(state.trigger).toHaveBeenCalledWith("hashchange");
            state.trigger.reset();

            state.setPoints([]);
            expect(state.trigger).toHaveBeenCalledWith("hashchange");
            state.trigger.reset();
        });

        it("fires a 'statuschange' event with status 'connecting' on refreshConfiguration()", function () {
            spyOn(state, "trigger");
            state.refreshConfiguration();
            expect(state.trigger).toHaveBeenCalledWith("statuschange", "connecting");
        });

        it("fires a 'statuschange' event with status 'connected' on refreshConfiguration().done()", function () {
            var isDone = false;
            spyOn(state, "trigger");
            
            runs(function () {
                state.refreshConfiguration().done(function () {
                    isDone = true;
                });
            });

            waitsFor(function () {
                return isDone;
            }, "refreshConfiguration() should successfully be done()", 3000);

            runs(function () {
                expect(state.trigger).toHaveBeenCalledWith("statuschange", "connected");
            });
        });

        it("fires a 'statuschange' event with status 'failed' on refreshConfiguration().fail()", function () {
            state = new FC.ClientState({
                url: "test",
                getConfiguration: FC.getConfiguration
            });

            var isFail = false;
            var errorMessage;
            spyOn(state, "trigger");
            
            runs(function () {
                state.refreshConfiguration().fail(function (error) {
                    isFail = true;
                    errorMessage = error.responseText;
                });
            });

            waitsFor(function () {
                return isFail;
            }, "refreshConfiguration() should fail()", 3000);

            runs(function () {
                expect(state.trigger).toHaveBeenCalledWith("statuschange", "failed: " + errorMessage);
            });
        });
    });
});