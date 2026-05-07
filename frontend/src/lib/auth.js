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
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({
        firstName:  profile.firstName,
        lastName:   profile.lastName,
        phone:      profile.phone,
        jobTitle:   profile.jobTitle,
        department: profile.department
      })
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

export async function searchBrokerCompanies(fca) {
  try {
    const params = fca ? `?fca=${encodeURIComponent(fca)}` : '';
    const response = await fetch(`${API_BASE}/company/brokers${params}`);
    if (!response.ok) return [];
    return await response.json();
  } catch {
    return [];
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

export async function fetchMyCompany(token) {
  try {
    const response = await fetch(`${API_BASE}/company/mine`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch company details.' };
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

export async function fetchUserById(id, token) {
  try {
    const response = await fetch(`${API_BASE}/auth/users/${id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch user.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function adminCreateUser(data, token) {
  try {
    const response = await fetch(`${API_BASE}/auth/users`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify(data)
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to create user.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function setUserLockout(id, locked, token) {
  try {
    const response = await fetch(`${API_BASE}/auth/users/${id}/lockout`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ locked })
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to update lockout.' };
    }
    return {};
  } catch (error) {
    return { error: error.message };
  }
}

export async function adminDeleteUser(id, token) {
  try {
    const response = await fetch(`${API_BASE}/auth/users/${id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to delete user.' };
    }
    return {};
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchApplications(token) {
  try {
    const response = await fetch(`${API_BASE}/application`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch applications.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchApplication(id, token) {
  try {
    const response = await fetch(`${API_BASE}/application/${id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to fetch application.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function createApplication(formSchemaId, token) {
  try {
    const response = await fetch(`${API_BASE}/application`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ formSchemaId })
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to create application.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function updateApplication(id, data, token) {
  try {
    const response = await fetch(`${API_BASE}/application/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify(data)
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to update application.' };
    }
    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function linkWorkflowToSchema(schemaId, workflowId, token) {
  try {
    const response = await fetch(`${API_BASE}/formbuilder/schema/${encodeURIComponent(schemaId)}/workflow`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ workflowId: workflowId ?? null })
    });
    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to link workflow.' };
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
      return { error: message || 'Unable to archive schema.' };
    }

    return {};
  } catch (error) {
    return { error: error.message };
  }
}

export async function fetchArchivedSchemas(token) {
  try {
    const response = await fetch(`${API_BASE}/formbuilder/schemas/archived`, {
      headers: { Authorization: `Bearer ${token}` }
    });

    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to load archived schemas.' };
    }

    return await response.json();
  } catch (error) {
    return { error: error.message };
  }
}

export async function restoreSchema(id, token) {
  try {
    const response = await fetch(`${API_BASE}/formbuilder/schema/${encodeURIComponent(id)}/restore`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` }
    });

    if (!response.ok) {
      const message = await response.text();
      return { error: message || 'Unable to restore schema.' };
    }

    return {};
  } catch (error) {
    return { error: error.message };
  }
}

// ── Business rules ────────────────────────────────────────────────────────────

export async function fetchRules(token, formSchemaId = null) {
  try {
    const qs = formSchemaId ? `?formSchemaId=${encodeURIComponent(formSchemaId)}` : '';
    const res = await fetch(`${API_BASE}/rules${qs}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    return res.ok ? await res.json() : { error: await res.text() };
  } catch (e) { return { error: e.message }; }
}

export async function saveRule(id, payload, token) {
  try {
    const res = await fetch(id ? `${API_BASE}/rules/${id}` : `${API_BASE}/rules`, {
      method: id ? 'PUT' : 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify(payload)
    });
    if (!res.ok) return { error: (await res.json()).error || await res.text() };
    return await res.json();
  } catch (e) { return { error: e.message }; }
}

export async function deleteRule(id, token) {
  try {
    const res = await fetch(`${API_BASE}/rules/${id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` }
    });
    return res.ok ? {} : { error: await res.text() };
  } catch (e) { return { error: e.message }; }
}

// ── Checklist ─────────────────────────────────────────────────────────────────

// ── Templates ─────────────────────────────────────────────────────────────────

export async function fetchTemplates(token, type = null) {
  try {
    const qs = type ? `?type=${encodeURIComponent(type)}` : '';
    const res = await fetch(`${API_BASE}/templates${qs}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!res.ok) return { error: await res.text() || 'Failed to load templates.' };
    return await res.json();
  } catch (e) { return { error: e.message }; }
}

export async function fetchTemplate(id, token) {
  try {
    const res = await fetch(`${API_BASE}/templates/${id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!res.ok) return { error: 'Template not found.' };
    return await res.json();
  } catch (e) { return { error: e.message }; }
}

export async function saveTemplate(template, token) {
  const method = template.id ? 'PUT' : 'POST';
  const url = template.id ? `${API_BASE}/templates/${template.id}` : `${API_BASE}/templates`;
  try {
    const res = await fetch(url, {
      method,
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify(template)
    });
    if (!res.ok) { const b = await res.json().catch(() => ({})); return { error: b.error || 'Failed to save template.' }; }
    return await res.json();
  } catch (e) { return { error: e.message }; }
}

export async function deleteTemplate(id, token) {
  try {
    const res = await fetch(`${API_BASE}/templates/${id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!res.ok) return { error: 'Failed to delete template.' };
    return {};
  } catch (e) { return { error: e.message }; }
}

export async function compileMjml(mjml, token) {
  try {
    const res = await fetch(`${API_BASE}/templates/compile-mjml`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ mjml })
    });
    const body = await res.json();
    if (!res.ok) return { error: body.error || 'Compilation failed.' };
    return body; // { html }
  } catch (e) { return { error: e.message }; }
}

export async function fetchChecklist(token, formSchemaId = null) {
  try {
    const qs = formSchemaId ? `?formSchemaId=${encodeURIComponent(formSchemaId)}` : '';
    const res = await fetch(`${API_BASE}/checklist${qs}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!res.ok) return { error: await res.text() || 'Failed to load checklist.' };
    return await res.json();
  } catch (e) { return { error: e.message }; }
}

export async function saveChecklistItem(item, token) {
  const method = item.id ? 'PUT' : 'POST';
  const url = item.id ? `${API_BASE}/checklist/${item.id}` : `${API_BASE}/checklist`;
  try {
    const res = await fetch(url, {
      method,
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify(item)
    });
    if (!res.ok) {
      const body = await res.json().catch(() => ({}));
      return { error: body.error || 'Failed to save item.' };
    }
    return await res.json();
  } catch (e) { return { error: e.message }; }
}

export async function deleteChecklistItem(id, token) {
  try {
    const res = await fetch(`${API_BASE}/checklist/${id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!res.ok) return { error: 'Failed to delete item.' };
    return {};
  } catch (e) { return { error: e.message }; }
}

export async function fetchApplicationChecklist(applicationId, token) {
  try {
    const res = await fetch(`${API_BASE}/checklist/application/${applicationId}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!res.ok) return { error: await res.text() || 'Failed to load checklist.' };
    return await res.json();
  } catch (e) { return { error: e.message }; }
}

export async function generateApplicationChecklist(applicationId, token) {
  try {
    const res = await fetch(`${API_BASE}/checklist/application/${applicationId}/generate`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!res.ok) return { error: 'Failed to generate checklist.' };
    return {};
  } catch (e) { return { error: e.message }; }
}

export async function respondToChecklistItem(itemId, text, token) {
  try {
    const res = await fetch(`${API_BASE}/checklist/application/item/${itemId}/respond`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ text })
    });
    if (!res.ok) return { error: 'Failed to save response.' };
    return {};
  } catch (e) { return { error: e.message }; }
}

export async function uploadChecklistDocument(itemId, file, token) {
  try {
    const form = new FormData();
    form.append('file', file);
    const res = await fetch(`${API_BASE}/checklist/application/item/${itemId}/upload`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
      body: form
    });
    if (!res.ok) return { error: 'Failed to upload document.' };
    return {};
  } catch (e) { return { error: e.message }; }
}

export async function updateChecklistItemStatus(itemId, status, token) {
  try {
    const res = await fetch(`${API_BASE}/checklist/application/item/${itemId}/status`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ status })
    });
    if (!res.ok) { const b = await res.json().catch(() => ({})); return { error: b.error || 'Failed to update status.' }; }
    return {};
  } catch (e) { return { error: e.message }; }
}

export async function addChecklistComment(itemId, comment, token) {
  try {
    const res = await fetch(`${API_BASE}/checklist/application/item/${itemId}/comment`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ comment })
    });
    if (!res.ok) { const b = await res.json().catch(() => ({})); return { error: b.error || 'Failed to add comment.' }; }
    return {};
  } catch (e) { return { error: e.message }; }
}

export async function evaluateRules(formSchemaId, formData, token, applicationId = null, currentStageId = null) {
  try {
    const res = await fetch(`${API_BASE}/rules/evaluate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ formSchemaId, formData, applicationId, currentStageId })
    });
    return res.ok ? await res.json() : { error: await res.text() };
  } catch (e) { return { error: e.message }; }
}

export async function fetchRuleOutcomes(applicationId, token) {
  try {
    const res = await fetch(`${API_BASE}/rules/outcomes/${applicationId}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    return res.ok ? await res.json() : { error: await res.text() };
  } catch (e) { return { error: e.message }; }
}
