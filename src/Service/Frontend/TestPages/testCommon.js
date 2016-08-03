function arrayInfoToString(a) {
    var r = "Values range = [" + a.min + "," + a.max + "]; Array shape = ";
    var s = "";
    while ($.isArray(a)) {
        if (s.length > 0)
            s += "x";
        s = s + a.length;
        a = a[0];
    }
    return r + s;
}

function jaggedArraySize(a) {
    var r = 1;
    while ($.isArray(a)) {
        r = r * a.length;
        a = a[0];
    }
    return r;
}

