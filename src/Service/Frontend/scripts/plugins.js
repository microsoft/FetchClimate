(function () {
    "use strict";

    /**
    * Fuzzy searches search query in a string.
    *
    * Returns true if query fuzzy included in tring, otherwise false.
    */
    String.prototype.fuzzy = function (query) {
        var source = this.toLowerCase(),
            i = 0,
            index = 0,
            character;
        query = query.toLowerCase();

        for (; character = query[i++];) {
            if (-1 === (index = source.indexOf(character, index))) {
                return false;
            }
            index++;
        }
        return true;
    };


    /**
    * Replace character at given index with new string.
    *
    * Return changed string.
    */
    String.prototype.replaceAt = function (index, str) {
        return this.substr(0, index) + str + this.substr(index + 1);
    };
})();