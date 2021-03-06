function coerceToBase64Url(thing, name) {
    if (Array.isArray(thing)) {
        thing = Uint8Array.from(thing);
    }
    if (thing instanceof ArrayBuffer) {
        thing = new Uint8Array(thing);
    }
    if (thing instanceof Uint8Array) {
        var str = '';
        var len = thing.byteLength;
        for (var i = 0; i < len; i++) {
            str += String.fromCharCode(thing[i]);
        }
        thing = window.btoa(str);
    }
    if (typeof thing !== 'string') {
        throw new Error('could not coerce \'' + (name || 'input') + '\' to string');
    }
    thing = thing.replace(/\+/g, '-').replace(/\//g, '_').replace(/=*$/g, '');
    return thing;
}
async function registerNewCredential() {
    var data = $('form#integrated-credentials').serialize();
    console.log('integrated-credentials: ');
    console.log(data);
    let makeCredentialOptions;
    try {
        let url = $('#makeCredentialOptions').attr('href');
        makeCredentialOptions = await $.ajax({
            url: url,
            type: 'post',
            data: data
        });
        makeCredentialOptions.challenge = Uint8Array.from(atob(makeCredentialOptions.challenge), c => c.charCodeAt(0));
        makeCredentialOptions.user.id = Uint8Array.from(atob(makeCredentialOptions.user.id), c => c.charCodeAt(0));
    }
    catch (e) {
        console.error(e);
        return;
    }
    let newCredential;
    try {
        newCredential = await navigator.credentials.create({
            publicKey: makeCredentialOptions
        });
    }
    catch (e) {
        var msg = "Could not create credentials in browser. Probably because the username is already registered with your authenticator. Please change username or authenticator.";
        console.error(msg);
        console.error(e);
        return;
    }
    if (newCredential) {
        let attestationObject = new Uint8Array(newCredential.response.attestationObject);
        let clientDataJSON = new Uint8Array(newCredential.response.clientDataJSON);
        let rawId = new Uint8Array(newCredential.rawId);
        const credData = {
            id: newCredential.id,
            rawId: coerceToBase64Url(rawId, 'rawId'),
            type: newCredential.type,
            extensions: newCredential.getClientExtensionResults(),
            response: {
                AttestationObject: coerceToBase64Url(attestationObject, 'attestationObject'),
                clientDataJson: coerceToBase64Url(clientDataJSON, 'clientDataJSON')
            }
        };
        try {
            let url = $('#makeCredentials').attr('href') + '?' + makeCredentialOptions.serialize();
            console.log(url);
            let cred = await $.ajax({
                url: url,
                type: 'post',
                data: credData
            });
            console.log(cred);
        }
        catch (e) {
            console.error(e);
            return;
        }
    }
}
async function loginViaIntegratedCredentials(el) {
    let $form = $(el).closest('form');
    let email = $form.find('input[name=EmailAddress]').val() || '';
    if (email.trim().length === 0) {
        $('[data-valmsg-for="EmailAddress"]').text('Email required');
        return;
    }
    else {
        $('[data-valmsg-for="EmailAddress"]').text('');
    }
    let makeAssertionOptions;
    try {
        let url = $('#assertionOptions').attr('href');
        makeAssertionOptions = await $.ajax({
            url: url,
            type: 'post',
            data: 'email=' + encodeURIComponent(email)
        });
    }
    catch (e) {
        console.log("Request to server failed");
        console.log(e);
        return;
    }
    if (makeAssertionOptions.status !== "ok") {
        console.log("Error creating assertion options");
        console.log(makeAssertionOptions.errorMessage);
        return;
    }
    const challenge = makeAssertionOptions.challenge.replace(/-/g, "+").replace(/_/g, "/");
    makeAssertionOptions.challenge = Uint8Array.from(atob(challenge), c => c.charCodeAt(0));
    makeAssertionOptions.allowCredentials.forEach(function (listItem) {
        var fixedId = listItem.id.replace(/\_/g, "/").replace(/\-/g, "+");
        listItem.id = Uint8Array.from(atob(fixedId), c => c.charCodeAt(0));
    });
    confirm('Tap your security key to login.');
    let credential;
    try {
        credential = await navigator.credentials.get({ publicKey: makeAssertionOptions });
    }
    catch (err) {
        console.log(err.message ? err.message : err);
        return;
    }
    try {
        await verifyLoginAssertionWithServer(credential);
    }
    catch (e) {
        console.error("Could not verify assertion");
        console.error(e);
    }
}
async function verifyLoginAssertionWithServer(assertedCredential) {
    let authData = new Uint8Array(assertedCredential.response.authenticatorData);
    let clientDataJSON = new Uint8Array(assertedCredential.response.clientDataJSON);
    let rawId = new Uint8Array(assertedCredential.rawId);
    let sig = new Uint8Array(assertedCredential.response.signature);
    const data = {
        id: assertedCredential.id,
        rawId: coerceToBase64Url(rawId, 'rawId'),
        type: assertedCredential.type,
        extensions: assertedCredential.getClientExtensionResults(),
        response: {
            authenticatorData: coerceToBase64Url(authData, 'authData'),
            clientDataJson: coerceToBase64Url(clientDataJSON, 'clientDataJSON'),
            signature: coerceToBase64Url(sig, 'sig')
        }
    };
    let response;
    try {
        let url = $('#makeAssertion').attr('href');
        response = await $.ajax({
            url: url,
            type: 'post',
            data: data
        });
    }
    catch (e) {
        console.error("Request to server failed", e);
        throw e;
    }
    console.log("Assertion Object", response);
    if (response.status !== "ok") {
        console.log("Error doing assertion");
        console.log(response.errorMessage);
        return;
    }
    location.assign('/');
}
//# sourceMappingURL=fido.js.map