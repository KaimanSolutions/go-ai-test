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

export async function startSSO(provider = 'generic') {
  const url = `${API_BASE}/auth/sso?provider=${encodeURIComponent(provider)}`;
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

export async function fetchUserProfile(token) {
  try {
    const response = await fetch(`${API_BASE}/profile`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch profile.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function saveUserProfile(profile, token) {
  try {
    const response = await fetch(`${API_BASE}/profile`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`
      },
      body: JSON.stringify(profile)
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to save profile.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function register(data) {
  try {
    const response = await fetch(`${API_BASE}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Registration failed.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchNetworks() {
  try {
    const response = await fetch(`${API_BASE}/company/networks`);
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch networks.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchCompanies() {
  try {
    const response = await fetch(`${API_BASE}/company`);
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch companies.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchCompanyDetail(id) {
  try {
    const response = await fetch(`${API_BASE}/company/${id}`);
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch company details.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchFcaCompany(frn) {
  try {
    const response = await fetch(`${API_BASE}/company/fca-lookup/${encodeURIComponent(frn)}`);
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to lookup FCA firm.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchUsers(token) {
  try {
    const response = await fetch(`${API_BASE}/auth/users`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch users.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function registerCompany(data) {
  try {
    const response = await fetch(`${API_BASE}/company`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Company registration failed.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchIntegrationServices(token) {
  try {
    const response = await fetch(`${API_BASE}/integrations`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch integration services.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function updateIntegrationCredentials(integration, credentials, token) {
  try {
    const response = await fetch(`${API_BASE}/integrations/${encodeURIComponent(integration)}/credentials`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`
      },
      body: JSON.stringify(credentials)
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Failed to update credentials.' };
    }
    return {};
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchApiLogs(token, { integration = '', page = 1, pageSize = 50 } = {}) {
  try {
    const params = new URLSearchParams({ page, pageSize });
    if (integration) params.set('integration', integration);
    const response = await fetch(`${API_BASE}/apilogs?${params}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch API logs.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchApiLogDetail(id, token) {
  try {
    const response = await fetch(`${API_BASE}/apilogs/${id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch log detail.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchApiIntegrations(token) {
  try {
    const response = await fetch(`${API_BASE}/apilogs/integrations`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) return { error: 'Unable to fetch integrations.' };
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function addCompanyBankDetails(companyId, data) {
  try {
    const response = await fetch(`${API_BASE}/company/${companyId}/bank-details`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Failed to save bank details.' };
    }
    return {};
  } catch (error) {
    return { error: error.message };
  }
}

export async function deleteCompanyBankDetails(companyId, bankDetailsId) {
  try {
    const response = await fetch(`${API_BASE}/company/${companyId}/bank-details/${bankDetailsId}`, {
      method: 'DELETE'
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Failed to delete bank account.' };
    }
    return {};
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchWorkflows(token) {
  try {
    const response = await fetch(`${API_BASE}/workflow`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch workflows.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchWorkflow(id, token) {
  try {
    const response = await fetch(`${API_BASE}/workflow/${id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch workflow.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function saveWorkflow(id, data, token) {
  try {
    const response = await fetch(id ? `${API_BASE}/workflow/${id}` : `${API_BASE}/workflow`, {
      method: id ? 'PUT' : 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify(data)
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to save workflow.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function deleteWorkflow(id, token) {
  try {
    const response = await fetch(`${API_BASE}/workflow/${id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to delete workflow.' };
    }
    return {};
  } catch (error) {
    return { error: error.message };
  }
}

export async function deleteSchema(id, token) {
  try {
    const response = await fetch(`${API_BASE}/formbuilder/schema/${encodeURIComponent(id)}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` }
    });

    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to delete schema.' };
    }

    return {};
  } catch (error) {
    return { error: error.message };
  }
}
