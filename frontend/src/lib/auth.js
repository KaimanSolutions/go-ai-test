const API_BASE = '/api';

export async function login(username, password) {
  try {
    const response = await fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });

    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Login failed.' };
    }

    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function startSSO() {
  const url = `${API_BASE}/auth/sso`;
  window.location.href = url;
}

export async function fetchBrandingSettings() {
  try {
    const response = await fetch(`${API_BASE}/settings/branding`);
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch branding settings.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function saveBrandingSettings(settings, token) {
  try {
    const response = await fetch(`${API_BASE}/settings/branding`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`
      },
      body: JSON.stringify(settings)
    });

    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to save branding settings.' };
    }

    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchSchemas() {
  try {
    const response = await fetch(`${API_BASE}/formbuilder/schemas`);
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch schemas.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchSchema(id) {
  try {
    const response = await fetch(`${API_BASE}/formbuilder/schema/${encodeURIComponent(id)}`);
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch form schema.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function saveSchema(schema, token) {
  try {
    const response = await fetch(`${API_BASE}/formbuilder/schema`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`
      },
      body: JSON.stringify(schema)
    });

    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to save schema.' };
    }

    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function validateSubmission(schema, values) {
  try {
    const response = await fetch(`${API_BASE}/formbuilder/validate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ schema, values })
    });

    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Validation failed.' };
    }

    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}
