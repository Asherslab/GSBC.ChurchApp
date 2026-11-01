const ceremony = (path, body) => fetch(`/accounts/passkeys/${path}`, {
  method: 'POST',
  credentials: 'same-origin',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(body),
});

const problem = async (response) => {
  try {
    const details = await response.json();
    return details.detail || details.title || `Something went wrong (${response.status}).`;
  } catch {
    return `Something went wrong (${response.status}).`;
  }
};

const failure = (error) => ({ ok: false, error });

const deviceName = () => {
  const platform = navigator.userAgentData?.platform || navigator.platform || 'This device';
  return platform.replace('MacIntel', 'Mac');
};

export const isSupported = () =>
  !!window.PublicKeyCredential && typeof PublicKeyCredential.parseCreationOptionsFromJSON === 'function';

export async function register(ticket) {
  const options = await ceremony('creation-options', { ticket });
  if (!options.ok) return failure(await problem(options));

  let credential;
  try {
    credential = await navigator.credentials.create({
      publicKey: PublicKeyCredential.parseCreationOptionsFromJSON(await options.json()),
    });
  } catch (error) {
    return failure(error.name === 'NotAllowedError' ? 'Cancelled.' : error.message);
  }

  const registered = await ceremony('register', { credential: JSON.stringify(credential.toJSON()), ticket, name: deviceName() });
  return registered.ok ? { ok: true, error: null } : failure(await problem(registered));
}

export async function signIn() {
  const options = await ceremony('request-options', {});
  if (!options.ok) return failure(await problem(options));

  let credential;
  try {
    credential = await navigator.credentials.get({
      publicKey: PublicKeyCredential.parseRequestOptionsFromJSON(await options.json()),
    });
  } catch (error) {
    return failure(error.name === 'NotAllowedError' ? 'Cancelled.' : error.message);
  }

  const signedIn = await ceremony('sign-in', { credential: JSON.stringify(credential.toJSON()) });
  return signedIn.ok ? { ok: true, error: null } : failure(await problem(signedIn));
}
