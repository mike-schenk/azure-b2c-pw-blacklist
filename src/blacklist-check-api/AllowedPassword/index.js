var crypto = require('crypto');

module.exports = async function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');

    if (req.body && req.body.password) {
        const password = req.body.password;

        var sha1Hasher = crypto.createHash('sha1');
        sha1Hasher.update(password, 'utf8');
        var fullShaString = sha1Hasher.digest('hex').toUpperCase();
        var shaPrefix = fullShaString.substring(0, 5);
        var shaSuffix = fullShaString.substring(5);

        // retrieve the pwned password hash suffixes for this prefix.
        var candidates = await getContent('https://api.pwnedpasswords.com/range/' + shaPrefix);
        var foundAt = candidates.indexOf(shaSuffix);
        if(foundAt >= 0) {
            context.res = {
                status: 409
            }
        }
        else {
            context.res = {
                status: 200
            }
        }
/*
        if(password === "123456") {
            context.res = {
                status: 409
            }
        }
        else {
            context.res = {
                status: 200
            }
        }
*/
    }
    else {
        context.res = {
            status: 400,
            body: "Submit a password in the request body"
        };
    }
};

const getContent = function(url) {
    // return new pending promise
    return new Promise((resolve, reject) => {
      // select http or https module, depending on reqested url
      const lib = url.startsWith('https') ? require('https') : require('http');
      const request = lib.get(url, {rejectUnauthorized: false}, (response) => {
        // handle http errors
        if (response.statusCode < 200 || response.statusCode > 299) {
           reject(new Error('Failure from remote call. Status code: ' + response.statusCode));
         }
        // temporary data holder
        const body = [];
        // on every content chunk, push it to the data array
        response.on('data', (chunk) => body.push(chunk));
        // we are done, resolve promise with those joined chunks
        response.on('end', () => resolve(body.join('')));
      });
      // handle connection errors of the request
      request.on('error', (err) => reject(err))
      })
  };