module.exports = async function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');

    if (req.body && req.body.password) {
        const password = req.body.password;
        //TODO: implement real blacklist check.
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
    }
    else {
        context.res = {
            status: 400,
            body: "Submit a password in the request body"
        };
    }
};