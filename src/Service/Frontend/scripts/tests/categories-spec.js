/// <reference path="..\external\jquery-2.0.3.min.js"/>
/// <reference path="..\request.js"/>

describe("FC.getCategoriesFromDescription", function () {
    it("returns empty array if no categories present", function () {
        var d = "This is an environmental variable";
        var r = FC.getCategoriesFromDescription(d);
        expect(r.description).toEqual(d);
        expect(r.categories).toEqual([]);
    });

    it("understands Category: prefix", function () {
        var r = FC.getCategoriesFromDescription("This is an environmental variable. Category: Environment");
        expect(r.description).toEqual("This is an environmental variable.");
        expect(r.categories).toEqual(["Environment"]);
    });

    it("understands Categories: prefix", function () {
        var r = FC.getCategoriesFromDescription("This is an environmental variable. Categories: Environment");
        expect(r.description).toEqual("This is an environmental variable.");
        expect(r.categories).toEqual(["Environment"]);
    });

    it("removes trailing semicolon", function () {
        var r = FC.getCategoriesFromDescription("This is an environmental variable; Categories: Environment");
        expect(r.description).toEqual("This is an environmental variable");
        expect(r.categories).toEqual(["Environment"]);
    });


    it("understands multiple categories", function () {
        var r = FC.getCategoriesFromDescription("This is an environmental variable. Categories: Environment, Sample");
        expect(r.description).toEqual("This is an environmental variable.");
        expect(r.categories).toEqual(["Environment", "Sample"]);
    });

    it("handles null", function () {
        var r = FC.getCategoriesFromDescription(null);
        expect(r.description).toEqual("");
        expect(r.categories).toEqual([]);
    });

    it("handles empty string", function () {
        var r = FC.getCategoriesFromDescription("");
        expect(r.description).toEqual("");
        expect(r.categories).toEqual([]);
    });

});