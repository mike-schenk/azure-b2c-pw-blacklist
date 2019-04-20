const values = require('./TenantPolicyValues.json');
const fs = require('fs');
const path = require('path');

const srcDir = path.join(__dirname, 'src', 'policies');
const srcFiles = fs.readdirSync(srcDir);

const destDir = path.join(__dirname, 'artifacts', 'policies');
fs.mkdirSync(destDir, {recursive: true});

while(srcFiles.length > 0) {
    const fileName = srcFiles.shift();
    const srcFileName = path.join(srcDir, fileName);
    let contents = fs.readFileSync(srcFileName, {encoding:'utf8'});
    contents = contents.replace(/{{tenant}}/g, values.tenant);
    contents = contents.replace(/{{ProxyIdentityExperienceFrameworkAppId}}/g, values.ProxyIdentityExperienceFrameworkAppId);
    contents = contents.replace(/{{IdentityExperienceFrameworkAppId}}/g, values.IdentityExperienceFrameworkAppId);
    const destFilePath = path.join(destDir, fileName);
    fs.writeFileSync(destFilePath, contents, {encoding: 'utf8'});
}