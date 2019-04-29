# azure-b2c-pw-blacklist
A sample extending an Azure B2C custom policy for blacklist checking against the haveibeenpwned API

## Demo

Try out this sample at https://b2c-pw-blacklist-sample.azurewebsites.net/

## Contents

This repository contains three parts in subdirectories of the `src` directory

* `blacklist-check-api` An Azure Function App in Node.js that is called by a "REST" technical profile in the policy files.
   * The function app implements the password blacklist check using https://haveibeenpwned.com/API/v2#PwnedPasswords
* `policies`: A set of Identity Experience Framework custom policy files.
* `webapp`: An ASP.Net Core web application to be the relying party.

## How it was implemented

### Disabled "strong password"

First, Azure AD B2C's "strong password" requirement had to be disabled.

In `TrustFrameworkBase.xml`:

* The regular expresion pattern on the restriction of the `newPassword` and `reenterPassword` claim types was changed.
  * To remove all the special character requirements, leaving only a min/max length check.
  * The maximum length was extended from 16 to 64 characters.
* "DisableStrongPassword" was added to the persisted `passwordPolicies` claim in both the `AAD-UserWriteUsingLogonEmail` and `AAD-UserWritePasswordUsingObjectId` technical profiles.

### Inserted the REST technical profile

Then, the signup and reset password flows were modified to call a new REST service in a validation technical profile before writing to the directory.

In `TrustFrameworkExtensions.xml`:

* A new technical profile, `API-AllowedPassword` was added.
  * It sends the raw (unhashed) password as a claim in the JSON body of a POST request to the blacklist API service whose source code is in this repository.
  * It _can_ send the error message to be displayed to the user. That message is reflected back in the API's response when the password is not allowed.
* The `LocalAccountSignUpWithLogonEmail` technical profile was extended to execute the `API-AllowedPassword` technical profile as a validation profile. It's important that the new validation profile executes _before_  `AAD-UserWriteUsingLogonEmail` which is the validation technical profile specified in the base.
* Just like the previous point, the `LocalAccountWritePasswordUsingObjectId` was extended with the new validation technical profile.

### Implemented the blacklist API

The blacklist of passwords is the over 550 million passwords collected at Troy Hunt's haveibeenpwned service. Because the haveibeenpwned API requires logic that an IEF technical profile can't do, that logic was written into the blacklist API Azure function app.

The technical profile calls this Azure function, passing the user's desired password. The function then performs the requisite hash and substring, calls https://api.pwnedpasswords.com and looks for a match. If a match is found, it returns a failing HTTP status code and "userMessage". When the technical profile gets the failing status code, it displays the user message to the end-user in the browser.

A localized or application-specific user message can optionally be supplied to the blacklist API through a `failMessage` claim.

## Possible improvements

* The blacklist API should check the password against a list of application-specific disallowed words.
  * That list shouldn't be hard-coded into the API, maybe it should be passed-in by the caller.
* The blacklist API should allow the caller to say that passwords which appeared up to _n_ times in breaches are still allowed.
* Call the blacklist API directly from the (customized) UI asynchronously so the user doesn't have to click "continue" before being told the password is disallowed.