class Url {
    constructor() {
        var queryParts = Url.ParseQueryParts(arguments);
        this.scheme = queryParts.scheme;
        this.login = (queryParts.login == null) ? null : decodeURIComponent(queryParts.login);
        this.password = (queryParts.password == null) ? null : decodeURIComponent(queryParts.password);
        this.host = decodeURIComponent(queryParts.host);
        this.port = queryParts.port;
        this.fragment = (queryParts.fragment == null) ? null : decodeURIComponent(queryParts.fragment);
        if (path == null || path.length == 0) {
            this.fullPath = "";
            this.parentpath = "";
            this.leafName = "";
            this.baseName = "";
            this.extension = "";
        } else {
            var pp = Uri.pathParts.exec(queryParts.path);
            this.fullPath = queryParts.path;
            this.parentpath = (typeof pp[1] === "undefined") ? "" : pp[1];
            this.leafName = (typeof pp[4] === "undefined") ? "" : decodeURIComponent(pp[4]);
            this.baseName = (typeof pp[5] === "undefined") ? "" : decodeURIComponent(pp[5]);
            this.extension = (typeof pp[7] === "undefined") ? "" : decodeURIComponent(pp[7]);
        }
        this.query = ParseQuery(queryParts.query);
    }
    toString() {
        var result = "";
        if (this.scheme.length > 0 || this.login !== null || this.password !== null || this.host.length > 0 || this.port !== null) {
            result = (this.scheme.length == 0) ? "http" : this.scheme;
            result = result + "://";
            if (this.login != null) {
                result = result + encodeURIComponent(this.login);
                if (this.password != null)
                    result = result + ":" + encodeURIComponent(this.password);
                result = result + "@";
            } else if (this.password != null)
                result = result + ":" + encodeURIComponent(this.password) + "@";
            result = result + this.host;
            if (this.port != null)
                result = result + ":" + String(this.port);
            if (this.fullPath.length > 0 && this.fullPath.substring(0, 1) != "/")
                result = result + "/";
        }
        result = result + this.fullPath;
        var q = Object.getOwnPropertyNames(this.query);
        for (var i = 0; i < q.length; i++) {
            var k = encodeURIComponent(q[i]);
            for (var n = 0; n < q[i].length; n++)
                q[i][n] = (typeof q[i][n] === "undefined" || q[i][n] === null) ? k : k + "=" + encodeURIComponent(q[i][n]);
            q[i] = q[i].join("&");
        }
        if (q.length > 0)
            result = result + "?" + q.join("&");
        return (this.fragment != null) ? result + "#" + encodeURIComponent(this.fragment) : result;
    }
    static AsPortNumber(value) {
        if (typeof value === "undefined" || value === null)
            return null;
        
        var i = parseInt(((typeof value === "string") ? value : String(value)).trim());
        if (isNaN(value) || i < 0 || i > 65535)
            return NaN;
        return value;
    }
    static ParseQuery(value) {
        var result = { };
        if (typeof value === undefined || value === null)
            return result;
        var pairs = String(value).split("&");
        for (var i = 0; i < pairs.length; i++) {
            var kvp = pairs[i].split("=", 2);
            var k = decodeURIComponent(kvp[0]);
            var v = (kvp.length == 1) ? null : decodeURIComponent(kvp[1]) ;
            if (typeof result[k] === undefined) {
                result[k] = [v];
            } else {
                result[k].push(v);
            }
        }
        return result;
    }
    static ParseQueryParts(values) {
        // 0: scheme, 1: login, 2: password, 3: host, 4: port [, 5: path, 6: query, 7: fragment]
        // 0: scheme, 1: login, 2: password, 3: host [, 4: path, 5: query, 6: fragment]
        // 0: scheme, 1: host, 2: port [, 3: path, 4: query, 5: fragment]
        // 0: scheme, 1: host [, 2: path, 3: query, 4: fragment]
        // 0: baseUri, 1: path [, 2: query, 3: fragment]
        // 0: uriString
        if (values.length == 0)
            return {
                'scheme': "",
                'login': null,
                'password': null,
                'host': "",
                'port': null,
                'path': "",
                'query': null,
                'fragment': null
            };
        if (values.length == 1) {
            if (typeof values[0] === "undefined" || values[0] === null)
                return {
                    'scheme': "",
                    'login': null,
                    'password': null,
                    'host': "",
                    'port': null,
                    'path': "",
                    'query': null,
                    'fragment': null
                };
            var s = (typeof values[0] === "string") ? values[0] : String(values[0]);
            if (s.length == 0)
            return Url.ParseQueryParts([null, null, null, null, null, null]);
            var p = Url.exp.uriParts.exec(values[0]);
            if (p === null)
                return Url.ParseQueryParts([null, null, null, values[0], null, null]);
            return Url.ParseQueryParts(p[2], p[5], p[7], p[8], p[10], p[11], p[13], p[15]);
        }
        if (values.length > 7) {
            var portNum = null;
            if (typeof values[4] !== "undefined" && values[4] !== null) {
                var p = (typeof values[4] === "string") ? values[4] : String(values[4]);
                portNum = parseInt(p);
                if (isNaN(portNum) || portNum < 0 || n > 65535)
                    throw new Error("Invalid port number");
            }
            return {
                'scheme': (typeof values[0] === "undefined" || values[0] === null) ? "" : ((typeof values[0] === "string") ? values[0] : String(values[0])),
                'login': (typeof values[1] === "undefined" || values[1] === null) ? null : ((typeof values[1] === "string") ? values[1] : String(values[1])),
                'password': (typeof values[2] === "undefined" || values[2] === null) ? null : ((typeof values[2] === "string") ? values[2] : String(values[2])),
                'host': (typeof values[3] === "undefined" || values[3] === null) ? "" : ((typeof values[3] === "string") ? values[3] : String(values[3])),
                'port': portNum,
                'path': (typeof values[5] === "undefined" || values[5] === null) ? "" : ((typeof values[5] === "string") ? values[5] : String(values[5])),
                'query': (typeof query === "undefined" || values[6] === null) ? null : ((typeof values[6] === "string") ? values[6] : String(values[6])),
                'fragment': (typeof values[7] === "undefined" || values[7] === null) ? null : ((typeof values[7] === "string") ? values[7] : String(values[7]))
            };
        }

        var s = (typeof values[0] === "undefined" || values[0] === null) ? "" : ((typeof values[0] === "string") ? values[0] : String(values[0]));
        if (Url.exp.scheme.test(s)) {
            if (values.length == 2)
                return Uri.ParseQueryParts([s, null, null, values[1], null , null, null, null]);
            if (values.length > 4 && typeof values[4] === "Number") {
                if (values.length == 5)
                    return Uri.ParseQueryParts([s, values[1], values[2], values[3], values[4], null, null, null]);
                return Uri.ParseQueryParts([s, values[1], values[2], values[3], values[4], values[5], (values.length == 6) ? null : values[6], null]);
            }
            if (values.length < 7 && typeof values[2] === "Number") {
                if (values.length == 3)
                    return Uri.ParseQueryParts([s, null, null, values[1], values[2], null, null, null]);
                if (values.length == 4)
                    return Uri.ParseQueryParts([s, null, null, values[1], values[2], values[3], null, null]);
                return Uri.ParseQueryParts([s, null, null, values[1], values[2], values[3], values[4], (values.length == 5) ? null : values[5]]);
            }
            if (values.length == 7)
                return Uri.ParseQueryParts([s, values[1], values[2], values[3], null, values[4], values[5], values[6]]);
            if (values.length == 6)
                return Uri.ParseQueryParts([s, values[1], values[2], values[3], null, values[4], values[5], null]);
            if (values.length == 5)
                return Uri.ParseQueryParts([s, null, null, values[1], null, values[2], values[3], values[4]]);
            return Uri.ParseQueryParts([s, null, null, values[1], null, values[2], (values.length == 3) ? null : values[3], null]);
        }
        if (values.length < 5) {
            var b = Uri.ParseQueryParts([values[0]]);
            if (values.length == 4)
                return Url.ParseQueryParts([b.scheme, b.login, b.password, b.host, b.port, values[1], values[2], values[3]]);
            if (values.length > 2)
                return Url.ParseQueryParts([b.scheme, b.login, b.password, b.host, b.port, values[1], (values.length == 3) ? values[2] : null]);
            return Url.ParseQueryParts([b.scheme, b.login, b.password, b.host, b.port, values[1], null, null]);
        }
        throw new Error("Invalid Scheme");
    }
    static IsValidScheme(scheme) {
        if (typeof scheme === "undefined" || scheme === null)
            return false;
        if (typeof scheme === "string")
            return Url.exp.scheme.test(scheme);
        return Url.exp.scheme.test(String(scheme));
        // scheme, host, port [, path, query, fragment]

        // scheme, host [, path, query, fragment]
        // scheme, host [, port, path, query]
        
        // baseUri [, path, query, fragment]
    }
}
Url.exp = {
    scheme: /^[^:/]+$/i,
    /*0='http://user:pass@site.com/path/subpath?query=me#frago'; 1='http://user:pass@site.com'; 2='http'; 3='user:pass@site.com'; 4='user:pass@'; 5='user'; 6=':pass'; 7='pass'; 8='site.com'; 9=undefined; 10=undefined; 11='/path/subpath'; 12='?query=me'; 13='query=me'; 14='#frago'; 15='frago'
    */
    uriParts: /^(([^:/]+):\/\/((([^:@/]*)(:([^@/]*))?@)?([^/?#:]+)(:(\d+))?)?)?([^?#]*)(\?([^#]*))?(\#(.*))?$/,
    pathParts: /^(\/*([^/]+\/+(?=[^/]))*)(((\.*[^/.]+(\.+[^/.]+(?=\.[^/.]))*)(\.*[^/.]+)?)\/*)$/
};
