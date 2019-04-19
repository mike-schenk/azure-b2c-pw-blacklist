# azure-b2c-pw-blacklist
A sample extending an Azure B2C custom policy for blacklist checking against the haveibeenpwned API

## Contents

This repository contains three parts:

1. A set of Identity Experience Framework custom policy files in the src/policies directory
2. A Node.js web application to be the relying party in the src/webapp directory
3. An Azure Function App in Node.js that is called from a technical profile in the policy files in the src/blacklist-check-api directory
   * The function app implements the password blacklist check using https://haveibeenpwned.com/API/v2#PwnedPasswords