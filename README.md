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
