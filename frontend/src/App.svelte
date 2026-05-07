<script>
  import { onMount } from 'svelte';
  import Auth from './components/Auth.svelte';
  import FormBuilder from './components/FormBuilder.svelte';
  import FormRenderer from './components/FormRenderer.svelte';
  import { login, startSSO, fetchBrandingSettings, saveBrandingSettings, fetchSchemas, fetchSchema, saveSchema, validateSubmission, deleteSchema, fetchArchivedSchemas, restoreSchema, fetchUsers, fetchWorkflows, linkWorkflowToSchema } from './lib/auth.js';
  import Settings from './components/Settings.svelte';
  import HelpCentre from './components/HelpCentre.svelte';
  import Profile from './components/Profile.svelte';
  import FormRunner from './components/FormRunner.svelte';
  import ClientPortal from './components/ClientPortal.svelte';
  import BrokerPortal from './components/BrokerPortal.svelte';
  import Companies from './components/Companies.svelte';
  import Users from './components/Users.svelte';
  import Integrations from './components/Integrations.svelte';
  import Workflows from './components/Workflows.svelte';
  import Applications from './components/Applications.svelte';
  import Rules from './components/Rules.svelte';
  import Checklist from './components/Checklist.svelte';
  import Templates from './components/Templates.svelte';
  import MyCompany from './components/MyCompany.svelte';
  import { register } from './lib/auth.js';

  let token = $state('');
  let user = $state(null);
  let error = $state('');
  let selectedPortal = $state(''); // 'Admin' | 'Client' | 'Broker'
  let selectedPage = $state('Dashboard');
  let clientPage = $state('dashboard');
  let brokerPage = $state('dashboard');

  function handleLogout() {
    token = '';
    user = null;
    selectedPortal = '';
    selectedPage = 'Dashboard';
    clientPage = 'dashboard';
    brokerPage = 'dashboard';
    error = '';
  }
  let schema = $state(null);
  let validationResult = null;
  let schemas = $state([]);
  let archivedSchemas = $state([]);
  let showArchived = $state(false);
  let selectedFormId = 'loan-application';
  let branding = $state({
    primaryColor: '#0ea5e9',
    accentColor: '#7c3aed',
    backgroundColor: '#0f172a',
    surfaceColor: '#111827',
    cardColor: '#1f2937',
    textColor: '#e2e8f0',
    mutedTextColor: '#94a3b8',
    fontBody: 'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif',
    fontHeading: 'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif'
  });
  let users     = $state({});
  let workflows = $state([]);
  const menuItems = ['Applications', 'Dashboard', 'Forms', 'Workflows', 'Rules', 'Checklist', 'Templates', 'Companies', 'Users', 'Settings', 'Integrations', 'Help Centre'];
  const fontOptions = [
    { value: 'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif', label: 'Inter' },
    { value: 'Roboto, sans-serif', label: 'Roboto' },
    { value: "'Open Sans', sans-serif", label: 'Open Sans' },
    { value: 'Lato, sans-serif', label: 'Lato' },
    { value: 'Montserrat, sans-serif', label: 'Montserrat' },
    { value: "'Source Sans Pro', sans-serif", label: 'Source Sans Pro' },
    { value: "'Nunito Sans', sans-serif", label: 'Nunito Sans' },
    { value: "'Poppins', sans-serif", label: 'Poppins' },
    { value: "'Work Sans', sans-serif", label: 'Work Sans' },
    { value: "'Fira Sans', sans-serif", label: 'Fira Sans' },
    { value: "'Helvetica Neue', Helvetica, Arial, sans-serif", label: 'Helvetica Neue' },
    { value: 'Arial, sans-serif', label: 'Arial' },
    { value: "'Times New Roman', Times, serif", label: 'Times New Roman' },
    { value: 'Georgia, serif', label: 'Georgia' },
    { value: "'Courier New', Courier, monospace", label: 'Courier New' }
  ];

  const menuIcons = {
    'Dashboard': 'M3 9l9-7 9 7v11a2 2 0 01-2 2h-4v-6H9v6H5a2 2 0 01-2-2V9z',
    'Forms': 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
    'Applications': 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01',
    'Users': 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z',
    'Settings': 'M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4',
    'Integrations': 'M11 4a2 2 0 114 0v1a1 1 0 001 1h3a1 1 0 011 1v3a1 1 0 01-1 1h-1a2 2 0 100 4h1a1 1 0 011 1v3a1 1 0 01-1 1h-3a1 1 0 01-1-1v-1a2 2 0 10-4 0v1a1 1 0 01-1 1H7a1 1 0 01-1-1v-3a1 1 0 00-1-1H4a2 2 0 110-4h1a1 1 0 001-1V7a1 1 0 011-1h3a1 1 0 001-1V4z',
    'Companies': 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4',
    'Workflows': 'M3.75 12h16.5m-16.5 3.75h16.5M3.75 19.5h16.5M5.625 4.5h12.75a1.875 1.875 0 010 3.75H5.625a1.875 1.875 0 010-3.75z',
    'Rules': 'M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z',
    'Checklist': 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4',
    'Templates': 'M3.75 9.776c.112-.017.227-.026.344-.026h15.812c.117 0 .232.009.344.026m-16.5 0a2.25 2.25 0 00-1.883 2.542l.857 6a2.25 2.25 0 002.227 1.932H19.05a2.25 2.25 0 002.227-1.932l.857-6a2.25 2.25 0 00-1.883-2.542m-16.5 0V6A2.25 2.25 0 016 3.75h3.879a1.5 1.5 0 011.06.44l2.122 2.12a1.5 1.5 0 001.06.44H18A2.25 2.25 0 0120.25 9v.776',
    'Help Centre': 'M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
    'Profile': 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z',
  };

  async function handleLogin(credentials) {
    error = '';
    const response = await login(credentials.username, credentials.password);
    if (response.error) {
      error = response.error;
      return;
    }
    token = response.accessToken;
    user = { userName: response.userName, email: response.email, role: response.role, companyId: response.companyId ?? null, companyName: response.companyName ?? null };
    selectedPage = 'Dashboard';
    if (response.role === 'Admin') {
      await loadSchema();
      await loadUsers();
      await loadWorkflows();
    }
  }

  async function handleRegister(data) {
    error = '';
    const response = await register(data);
    if (response.error) {
      error = response.error;
      return;
    }
    // Auto-login after registration
    await handleLogin({ username: data.email, password: data.password });
  }

  async function handleSSO(detail) {
    const provider = detail?.provider || 'generic';
    await startSSO(provider);
  }

  async function loadSchema() {
    const response = await fetchSchema(selectedFormId);
    if (response.error) {
      error = response.error;
      return;
    }
    schema = response;
  }

  async function loadBranding() {
    const response = await fetchBrandingSettings();
    if (response.error) {
      console.warn('Failed to load branding settings:', response.error);
      return;
    }
    branding.primaryColor = response.primaryColor;
    branding.accentColor = response.accentColor;
    branding.backgroundColor = response.backgroundColor;
    branding.surfaceColor = response.surfaceColor;
    branding.cardColor = response.cardColor;
    branding.textColor = response.textColor;
    branding.mutedTextColor = response.mutedTextColor;
    branding.fontBody = response.fontBody;
    branding.fontHeading = response.fontHeading;
  }

  async function loadSchemas() {
    const response = await fetchSchemas();
    if (response.error) {
      console.warn('Failed to load schemas:', response.error);
      return;
    }
    schemas = response;
  }

  async function loadArchivedSchemas() {
    const response = await fetchArchivedSchemas(token);
    if (!response.error) archivedSchemas = response;
  }

  async function loadUsers() {
    const response = await fetchUsers(token);
    if (response.error) {
      error = response.error;
      return;
    }
    users = response;
  }

  async function handleSave(updatedSchema) {
    const response = await saveSchema(updatedSchema, token);
    if (response.error) {
      error = response.error;
      return;
    }
    schema = response;
    await loadSchemas();
    selectedPage = 'Forms';
  }

  async function saveBranding() {
    const response = await saveBrandingSettings(branding, token);
    if (response.error) {
      error = response.error;
      return;
    }
    branding = response;
  }

  async function handleValidate(values) {
    if (!schema) return;
    validationResult = await validateSubmission(schema, values);
  }

  function createNewForm() {
    selectedFormId = 'new-' + Date.now();
    schema = { id: selectedFormId, title: 'New Form', description: '', steps: [] };
    selectedPage = 'Create Form';
  }

  function editForm(id) {
    selectedFormId = id;
    selectedPage = 'Create Form';
    loadSchema();
  }

  async function archiveForm(id) {
    if (!confirm('Archive this form? It can be restored later from the Archived Forms section.')) return;
    const response = await deleteSchema(id, token);
    if (response.error) {
      error = response.error;
      return;
    }
    schemas = schemas.filter(s => s.id !== id);
    await loadArchivedSchemas();
  }

  async function restoreForm(id) {
    const response = await restoreSchema(id, token);
    if (response.error) {
      error = response.error;
      return;
    }
    await loadSchemas();
    await loadArchivedSchemas();
  }

  async function loadWorkflows() {
    const res = await fetchWorkflows(token);
    if (!res.error) workflows = res;
  }

  onMount(async () => {
    await loadBranding();
  });

  // Load schemas only when the Forms page is actually opened
  $effect(() => {
    if (selectedPage === 'Forms' && token) {
      loadSchemas();
      loadArchivedSchemas();
    }
  });

  let styleVars = $derived(`--brand-primary: ${branding.primaryColor}; --brand-accent: ${branding.accentColor}; --brand-bg: ${branding.backgroundColor}; --brand-surface: ${branding.surfaceColor}; --brand-card: ${branding.cardColor}; --brand-text: ${branding.textColor}; --brand-muted: ${branding.mutedTextColor}; --font-body: ${branding.fontBody}; --font-heading: ${branding.fontHeading};`);

  let pageTitle = $derived(selectedPage === 'Create Form' ? 'Form Builder' : selectedPage);
</script>

<div style={styleVars}>
  {#if !token}
    <div class="relative flex min-h-screen items-center justify-center overflow-hidden bg-slate-950 p-6">
      <div class="absolute inset-0 bg-[radial-gradient(circle_at_top_left,_rgba(56,189,248,0.15),_transparent_35%),radial-gradient(circle_at_bottom_right,_rgba(168,85,247,0.12),_transparent_30%)]"></div>

      {#if !selectedPortal}
        <!-- Portal selector -->
        <div class="relative w-full max-w-3xl space-y-8">
          <div class="text-center">
            <p class="text-xs font-semibold uppercase tracking-[0.2em] text-sky-400">lend2me</p>
            <h2 class="mt-3 text-4xl font-semibold tracking-tight text-white">Welcome</h2>
            <p class="mt-3 text-slate-400">Select how you'd like to access the platform.</p>
          </div>
          <div class="grid gap-4 sm:grid-cols-3">
            <button
              onclick={() => { selectedPortal = 'Client'; error = ''; }}
              class="group flex flex-col items-start gap-4 rounded-3xl border border-slate-800 bg-slate-900/95 p-6 text-left shadow-xl transition hover:border-sky-500/50 hover:bg-slate-800/80"
            >
              <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-sky-500/20">
                <svg class="h-6 w-6 text-sky-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
                </svg>
              </div>
              <div>
                <p class="font-semibold text-white group-hover:text-sky-300 transition">Client portal</p>
                <p class="mt-1 text-xs leading-5 text-slate-400">Apply for a mortgage or manage your existing application.</p>
              </div>
            </button>

            <button
              onclick={() => { selectedPortal = 'Broker'; error = ''; }}
              class="group flex flex-col items-start gap-4 rounded-3xl border border-slate-800 bg-slate-900/95 p-6 text-left shadow-xl transition hover:border-violet-500/50 hover:bg-slate-800/80"
            >
              <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-violet-500/20">
                <svg class="h-6 w-6 text-violet-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z"/>
                </svg>
              </div>
              <div>
                <p class="font-semibold text-white group-hover:text-violet-300 transition">Broker portal</p>
                <p class="mt-1 text-xs leading-5 text-slate-400">Submit applications and manage your clients efficiently.</p>
              </div>
            </button>

            <button
              onclick={() => { selectedPortal = 'Admin'; error = ''; }}
              class="group flex flex-col items-start gap-4 rounded-3xl border border-slate-800 bg-slate-900/95 p-6 text-left shadow-xl transition hover:border-slate-600 hover:bg-slate-800/80"
            >
              <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-800">
                <svg class="h-6 w-6 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065zM15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                </svg>
              </div>
              <div>
                <p class="font-semibold text-white group-hover:text-slate-200 transition">Admin access</p>
                <p class="mt-1 text-xs leading-5 text-slate-400">Manage forms, settings, and platform configuration.</p>
              </div>
            </button>
          </div>
        </div>

      {:else}
        <!-- Auth form for selected portal -->
        <div class="relative w-full max-w-lg">
          <button
            onclick={() => { selectedPortal = ''; error = ''; }}
            class="mb-6 flex items-center gap-1.5 text-xs font-medium text-slate-400 hover:text-white transition"
          >
            <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd"/>
            </svg>
            Back
          </button>

          <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-8 shadow-2xl">
            <div class="mb-6">
              <span class="inline-block rounded-full px-3 py-1 text-xs font-semibold
                {selectedPortal === 'Client' ? 'bg-sky-500/20 text-sky-400' :
                 selectedPortal === 'Broker' ? 'bg-violet-500/20 text-violet-400' :
                 'bg-slate-800 text-slate-400'}">
                {selectedPortal} portal
              </span>
              <h3 class="mt-3 text-2xl font-semibold text-white">
                {selectedPortal === 'Admin' ? 'Admin sign in' : `${selectedPortal} access`}
              </h3>
              <p class="mt-1 text-sm text-slate-400">
                {selectedPortal === 'Admin'
                  ? 'Sign in with your admin credentials.'
                  : `Sign in or create a new ${selectedPortal.toLowerCase()} account.`}
              </p>
            </div>

            {#if error}
              <p class="mb-4 rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</p>
            {/if}

            <Auth
              portal={selectedPortal}
              onlogin={handleLogin}
              onregister={handleRegister}
              onsso={handleSSO}
            />
          </div>
        </div>
      {/if}
    </div>

  {:else if user?.role === 'Client'}
    <!-- Client portal -->
    <div class="flex h-screen overflow-hidden bg-slate-950">
      <aside class="flex h-full w-64 shrink-0 flex-col border-r border-slate-800 bg-slate-900">
        <div class="flex items-center gap-3 border-b border-slate-800 px-5 py-5">
          <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl bg-sky-500/20">
            <span class="text-sm font-bold text-sky-400">L</span>
          </div>
          <span class="text-base font-semibold text-white">lend2me</span>
        </div>
        <div class="flex-1 overflow-y-auto px-3 py-4">
          <p class="mb-2 px-3 text-xs font-semibold uppercase tracking-widest text-slate-600">Client portal</p>
          <div class="space-y-0.5">
            <button
              onclick={() => clientPage = 'dashboard'}
              class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition {clientPage === 'dashboard' ? 'bg-sky-500/10 text-sky-400' : 'text-slate-400 hover:bg-slate-800 hover:text-white'}"
            >
              <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
                <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2h-4v-6H9v6H5a2 2 0 01-2-2V9z"/>
              </svg>
              Dashboard
            </button>
            <button
              onclick={() => clientPage = 'applications'}
              class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition {clientPage === 'applications' ? 'bg-sky-500/10 text-sky-400' : 'text-slate-400 hover:bg-slate-800 hover:text-white'}"
            >
              <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
                <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01"/>
              </svg>
              My applications
            </button>
            <button
              onclick={() => clientPage = 'help'}
              class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition {clientPage === 'help' ? 'bg-sky-500/10 text-sky-400' : 'text-slate-400 hover:bg-slate-800 hover:text-white'}"
            >
              <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
                <path d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
              Help Centre
            </button>
          </div>
        </div>
        <div class="border-t border-slate-800 p-3 space-y-0.5">
          <button
            onclick={() => clientPage = 'profile'}
            class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 transition hover:bg-slate-800 {clientPage === 'profile' ? 'bg-slate-800' : ''}"
          >
            <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-sky-500/20 text-sm font-bold text-sky-400">
              {user.userName.charAt(0).toUpperCase()}
            </div>
            <div class="min-w-0 flex-1 text-left">
              <p class="truncate text-sm font-medium text-white">{user.userName}</p>
              <p class="truncate text-xs text-slate-500">Client</p>
            </div>
            <svg class="h-4 w-4 shrink-0 text-slate-600" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd"/>
            </svg>
          </button>
          <button
            onclick={handleLogout}
            class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm text-slate-400 transition hover:bg-slate-800 hover:text-red-400"
          >
            <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
              <path d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
            </svg>
            Sign out
          </button>
        </div>
      </aside>
      <div class="flex min-w-0 flex-1 flex-col overflow-hidden">
        <header class="flex shrink-0 items-center border-b border-slate-800 bg-slate-950/95 px-6 py-4">
          <h1 class="text-xl font-semibold text-white">
            {clientPage === 'profile' ? 'My Profile' : clientPage === 'help' ? 'Help Centre' : clientPage === 'applications' ? 'My Applications' : 'My Dashboard'}
          </h1>
        </header>
        <main class="flex-1 overflow-y-auto p-6">
          {#if clientPage === 'profile'}
            <Profile {token} {user} />
          {:else if clientPage === 'help'}
            <HelpCentre {token} role={user?.role} />
          {:else if clientPage === 'applications'}
            <Applications {token} {user} />
          {:else}
            <ClientPortal {user} onApplications={() => clientPage = 'applications'} />
          {/if}
        </main>
      </div>
    </div>

  {:else if user?.role === 'Broker'}
    <!-- Broker portal -->
    <div class="flex h-screen overflow-hidden bg-slate-950">
      <aside class="flex h-full w-64 shrink-0 flex-col border-r border-slate-800 bg-slate-900">
        <div class="flex items-center gap-3 border-b border-slate-800 px-5 py-5">
          <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl bg-violet-500/20">
            <span class="text-sm font-bold text-violet-400">L</span>
          </div>
          <span class="text-base font-semibold text-white">lend2me</span>
        </div>
        <div class="flex-1 overflow-y-auto px-3 py-4">
          <p class="mb-2 px-3 text-xs font-semibold uppercase tracking-widest text-slate-600">Broker portal</p>
          <div class="space-y-0.5">
            <button
              onclick={() => brokerPage = 'dashboard'}
              class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition {brokerPage === 'dashboard' ? 'bg-violet-500/10 text-violet-400' : 'text-slate-400 hover:bg-slate-800 hover:text-white'}"
            >
              <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
                <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2h-4v-6H9v6H5a2 2 0 01-2-2V9z"/>
              </svg>
              Dashboard
            </button>
            <button
              onclick={() => brokerPage = 'applications'}
              class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition {brokerPage === 'applications' ? 'bg-violet-500/10 text-violet-400' : 'text-slate-400 hover:bg-slate-800 hover:text-white'}"
            >
              <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
                <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01"/>
              </svg>
              Applications
            </button>
            <button
              onclick={() => brokerPage = 'company'}
              class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition {brokerPage === 'company' ? 'bg-violet-500/10 text-violet-400' : 'text-slate-400 hover:bg-slate-800 hover:text-white'}"
            >
              <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
                <path d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
              </svg>
              My Company
            </button>
            <button
              onclick={() => brokerPage = 'help'}
              class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition {brokerPage === 'help' ? 'bg-violet-500/10 text-violet-400' : 'text-slate-400 hover:bg-slate-800 hover:text-white'}"
            >
              <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
                <path d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
              Help Centre
            </button>
          </div>
        </div>
        <div class="border-t border-slate-800 p-3 space-y-0.5">
          <button
            onclick={() => brokerPage = 'profile'}
            class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 transition hover:bg-slate-800 {brokerPage === 'profile' ? 'bg-slate-800' : ''}"
          >
            <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-violet-500/20 text-sm font-bold text-violet-400">
              {user.userName.charAt(0).toUpperCase()}
            </div>
            <div class="min-w-0 flex-1 text-left">
              <p class="truncate text-sm font-medium text-white">{user.userName}</p>
              <p class="truncate text-xs text-slate-500">Mortgage Broker</p>
            </div>
            <svg class="h-4 w-4 shrink-0 text-slate-600" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd"/>
            </svg>
          </button>
          <button
            onclick={handleLogout}
            class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm text-slate-400 transition hover:bg-slate-800 hover:text-red-400"
          >
            <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
              <path d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
            </svg>
            Sign out
          </button>
        </div>
      </aside>
      <div class="flex min-w-0 flex-1 flex-col overflow-hidden">
        <header class="flex shrink-0 items-center border-b border-slate-800 bg-slate-950/95 px-6 py-4">
          <h1 class="text-xl font-semibold text-white">
            {brokerPage === 'profile' ? 'My Profile' : brokerPage === 'help' ? 'Help Centre' : brokerPage === 'applications' ? 'Applications' : brokerPage === 'company' ? 'My Company' : 'Broker Dashboard'}
          </h1>
        </header>
        <main class="flex-1 overflow-y-auto p-6">
          {#if brokerPage === 'profile'}
            <Profile {token} {user} />
          {:else if brokerPage === 'help'}
            <HelpCentre {token} role={user?.role} />
          {:else if brokerPage === 'applications'}
            <Applications {token} {user} />
          {:else if brokerPage === 'company'}
            <MyCompany {token} />
          {:else}
            <BrokerPortal {user} onApplications={() => brokerPage = 'applications'} />
          {/if}
        </main>
      </div>
    </div>

  {:else}
    <!-- Admin portal -->
    <div class="flex h-screen overflow-hidden bg-slate-950">

      <!-- Sidebar -->
      <aside class="flex h-full w-64 shrink-0 flex-col border-r border-slate-800 bg-slate-900">

        <!-- Logo -->
        <div class="flex items-center gap-3 border-b border-slate-800 px-5 py-5">
          <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl bg-sky-500/20">
            <span class="text-sm font-bold text-sky-400">L</span>
          </div>
          <span class="text-base font-semibold text-white">lend2me</span>
        </div>

        <!-- Nav -->
        <nav class="flex-1 overflow-y-auto px-3 py-4">
          <div class="space-y-0.5">
            {#each menuItems as item}
              <button
                onclick={() => selectedPage = item}
                class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition
                  {selectedPage === item || (item === 'Forms' && selectedPage === 'Create Form')
                    ? 'bg-sky-500/10 text-sky-400'
                    : 'text-slate-400 hover:bg-slate-800 hover:text-white'}"
              >
                <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
                  <path d={menuIcons[item] ?? ''}/>
                </svg>
                {item}
              </button>
            {/each}
          </div>
        </nav>

        <!-- User -->
        <div class="border-t border-slate-800 p-3 space-y-0.5">
          <button
            onclick={() => selectedPage = 'Profile'}
            class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 transition hover:bg-slate-800
              {selectedPage === 'Profile' ? 'bg-slate-800' : ''}"
          >
            <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-sky-500/20 text-sm font-bold text-sky-400">
              {user.userName.charAt(0).toUpperCase()}
            </div>
            <div class="min-w-0 flex-1 text-left">
              <p class="truncate text-sm font-medium text-white">{user.userName}</p>
              <p class="truncate text-xs text-slate-500">{user.email}</p>
            </div>
            <svg class="h-4 w-4 shrink-0 text-slate-600" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd"/>
            </svg>
          </button>
          <button
            onclick={handleLogout}
            class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm text-slate-400 transition hover:bg-slate-800 hover:text-red-400"
          >
            <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
              <path d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
            </svg>
            Sign out
          </button>
        </div>
      </aside>

      <!-- Main -->
      <div class="flex min-w-0 flex-1 flex-col overflow-hidden">

        <!-- Top bar -->
        <header class="flex shrink-0 items-center justify-between border-b border-slate-800 bg-slate-950/95 px-6 py-4">
          <h1 class="text-xl font-semibold text-white">{pageTitle}</h1>
          {#if selectedPage === 'Create Form'}
            <button
              onclick={() => selectedPage = 'Forms'}
              class="rounded-2xl border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-300 hover:bg-slate-800 transition"
            >Back to forms</button>
          {/if}
        </header>

        <!-- Error banner -->
        {#if error}
          <div class="shrink-0 border-b border-red-500/20 bg-red-500/10 px-6 py-3 text-sm font-medium text-red-400">{error}</div>
        {/if}

        <!-- Scrollable page content -->
        <main class="flex-1 overflow-y-auto p-6 space-y-6">

          {#if selectedPage === 'Dashboard'}
            <div class="grid gap-6 xl:grid-cols-3">
              <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
                <p class="text-sm text-slate-400">Total applications</p>
                <p class="mt-3 text-4xl font-semibold text-white">24</p>
              </div>
              <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
                <p class="text-sm text-slate-400">Pending reviews</p>
                <p class="mt-3 text-4xl font-semibold text-white">6</p>
              </div>
              <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
                <p class="text-sm text-slate-400">Active integrations</p>
                <p class="mt-3 text-4xl font-semibold text-white">3</p>
              </div>
            </div>
            <div class="grid gap-6 xl:grid-cols-2">
              <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
                <h3 class="text-lg font-semibold text-white">Recent activity</h3>
                <ul class="mt-4 space-y-3 text-sm text-slate-400">
                  <li>New mortgage form created</li>
                  <li>Application review pending from underwriting</li>
                  <li>Integration sync completed successfully</li>
                </ul>
              </div>
              <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
                <h3 class="text-lg font-semibold text-white">Quick actions</h3>
                <div class="mt-4 grid gap-3">
                  <button onclick={createNewForm} class="rounded-2xl bg-sky-500 px-4 py-3 text-sm font-semibold text-white transition hover:bg-sky-400">Create new form</button>
                  <button onclick={() => selectedPage = 'Applications'} class="rounded-2xl border border-slate-800 px-4 py-3 text-sm font-semibold text-slate-200 transition hover:bg-slate-800">Submit an application</button>
                </div>
              </div>
            </div>

          {:else if selectedPage === 'Forms'}
            <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
              <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <h3 class="text-base font-semibold text-white">Form schemas</h3>
                  <p class="mt-1 text-sm text-slate-400">Manage your form schemas.</p>
                </div>
                <button class="rounded-2xl bg-sky-500 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-sky-400" onclick={createNewForm}>Create new form</button>
              </div>
              <div class="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {#each schemas as s}
                  <div class="flex flex-col gap-4 rounded-3xl border border-slate-800 bg-slate-950/90 p-5">
                    <div>
                      <h4 class="text-base font-semibold text-white">{s.title}</h4>
                      <p class="mt-1.5 text-sm text-slate-400">{s.description}</p>
                    </div>

                    <!-- Workflow link -->
                    <div>
                      <label class="mb-1.5 block text-xs font-medium text-slate-500">Linked workflow</label>
                      <select
                        value={s.workflowId ?? ''}
                        onchange={async (e) => {
                          const wfId = e.target.value ? Number(e.target.value) : null;
                          const res = await linkWorkflowToSchema(s.id, wfId, token);
                          if (!res.error) await loadSchemas();
                        }}
                        class="w-full rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-xs text-white focus:border-sky-500 focus:outline-none"
                      >
                        <option value="">No workflow</option>
                        {#each workflows as w}
                          <option value={w.id}>{w.name}</option>
                        {/each}
                      </select>
                      {#if s.workflowName}
                        <p class="mt-1 text-xs text-emerald-400">✓ Applications auto-assigned to "{s.workflowName}"</p>
                      {/if}
                    </div>

                    <div class="flex gap-2 border-t border-slate-800 pt-3">
                      <button class="rounded-2xl bg-sky-500 px-3 py-2 text-xs font-semibold text-white transition hover:bg-sky-400" onclick={() => editForm(s.id)}>Edit</button>
                      <button class="rounded-2xl border border-amber-800/50 px-3 py-2 text-xs font-semibold text-amber-400 transition hover:bg-amber-950" onclick={() => archiveForm(s.id)}>Archive</button>
                    </div>
                  </div>
                {/each}
              </div>

              <!-- Archived forms -->
              {#if archivedSchemas.length > 0}
                <div class="mt-6 border-t border-slate-800 pt-6">
                  <button
                    class="flex items-center gap-2 text-sm font-medium text-slate-400 transition hover:text-slate-200"
                    onclick={() => showArchived = !showArchived}
                  >
                    <svg class="h-4 w-4 transition-transform {showArchived ? 'rotate-90' : ''}" viewBox="0 0 20 20" fill="currentColor">
                      <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd"/>
                    </svg>
                    Archived forms ({archivedSchemas.length})
                  </button>

                  {#if showArchived}
                    <div class="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                      {#each archivedSchemas as s}
                        <div class="flex flex-col gap-3 rounded-3xl border border-slate-800/60 bg-slate-950/50 p-5 opacity-70">
                          <div>
                            <h4 class="text-base font-semibold text-slate-300">{s.title}</h4>
                            <p class="mt-1 text-sm text-slate-500">{s.description}</p>
                            {#if s.archivedAt}
                              <p class="mt-1.5 text-xs text-slate-600">Archived {new Date(s.archivedAt).toLocaleDateString()}</p>
                            {/if}
                          </div>
                          <div class="border-t border-slate-800 pt-3">
                            <button class="rounded-2xl border border-emerald-800/50 px-3 py-2 text-xs font-semibold text-emerald-400 transition hover:bg-emerald-950" onclick={() => restoreForm(s.id)}>Restore</button>
                          </div>
                        </div>
                      {/each}
                    </div>
                  {/if}
                </div>
              {/if}
            </section>

          {:else if selectedPage === 'Users'}
            <Users {token} />

          {:else if selectedPage === 'Create Form'}
            <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
              <p class="mb-6 text-sm text-slate-400">Create or edit form schemas in a dedicated editor.</p>
              <FormBuilder {schema} onsave={handleSave} />
            </section>

          {:else if selectedPage === 'Applications'}
            <Applications {token} {user} />

          {:else if selectedPage === 'Workflows'}
            <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl" style="min-height: 70vh;">
              <Workflows {token} />
            </section>

          {:else if selectedPage === 'Rules'}
            <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl" style="min-height: 70vh;">
              <Rules {token} />
            </section>

          {:else if selectedPage === 'Checklist'}
            <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl" style="min-height: 70vh;">
              <Checklist {token} />
            </section>

          {:else if selectedPage === 'Templates'}
            <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl" style="min-height: 70vh;">
              <Templates {token} />
            </section>

          {:else if selectedPage === 'Companies'}
            <Companies />

          {:else if selectedPage === 'Settings'}
            <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
              <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <h3 class="text-base font-semibold text-white">Branding settings</h3>
                  <p class="mt-1 text-sm text-slate-400">Update colors, fonts, and theme options for your platform.</p>
                </div>
                <button class="rounded-2xl bg-sky-500 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-sky-400" onclick={saveBranding}>Save branding</button>
              </div>
              <div class="mt-6 grid gap-4 lg:grid-cols-2">
                <div>
                  <label for="primary-color" class="block text-sm font-medium text-white">Primary color</label>
                  <input id="primary-color" type="color" bind:value={branding.primaryColor} class="mt-2 h-12 w-full rounded-2xl border border-slate-800 bg-slate-950 p-3" />
                </div>
                <div>
                  <label for="accent-color" class="block text-sm font-medium text-white">Accent color</label>
                  <input id="accent-color" type="color" bind:value={branding.accentColor} class="mt-2 h-12 w-full rounded-2xl border border-slate-800 bg-slate-950 p-3" />
                </div>
                <div>
                  <label for="background-color" class="block text-sm font-medium text-white">Background color</label>
                  <input id="background-color" type="color" bind:value={branding.backgroundColor} class="mt-2 h-12 w-full rounded-2xl border border-slate-800 bg-slate-950 p-3" />
                </div>
                <div>
                  <label for="surface-color" class="block text-sm font-medium text-white">Surface color</label>
                  <input id="surface-color" type="color" bind:value={branding.surfaceColor} class="mt-2 h-12 w-full rounded-2xl border border-slate-800 bg-slate-950 p-3" />
                </div>
                <div>
                  <label for="text-color" class="block text-sm font-medium text-white">Text color</label>
                  <input id="text-color" type="color" bind:value={branding.textColor} class="mt-2 h-12 w-full rounded-2xl border border-slate-800 bg-slate-950 p-3" />
                </div>
                <div>
                  <label for="muted-text-color" class="block text-sm font-medium text-white">Muted text</label>
                  <input id="muted-text-color" type="color" bind:value={branding.mutedTextColor} class="mt-2 h-12 w-full rounded-2xl border border-slate-800 bg-slate-950 p-3" />
                </div>
                <div class="lg:col-span-2">
                  <label for="heading-font" class="block text-sm font-medium text-white">Heading font</label>
                  <select id="heading-font" bind:value={branding.fontHeading} class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 text-white">
                    {#each fontOptions as option}
                      <option value={option.value}>{option.label}</option>
                    {/each}
                  </select>
                </div>
                <div class="lg:col-span-2">
                  <label for="body-font" class="block text-sm font-medium text-white">Body font</label>
                  <select id="body-font" bind:value={branding.fontBody} class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 text-white">
                    {#each fontOptions as option}
                      <option value={option.value}>{option.label}</option>
                    {/each}
                  </select>
                </div>
              </div>
              <div class="mt-6 rounded-3xl border border-slate-800 bg-slate-950/90 p-6">
                <p class="text-base font-semibold text-brand-primary" style="font-family: var(--font-heading);">Live theme preview</p>
                <p class="mt-2 text-sm text-slate-400">Your current branding values are applied across the platform.</p>
              </div>
            </section>

          {:else if selectedPage === 'Integrations'}
            <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
              <Integrations {token} />
            </section>

          {:else if selectedPage === 'Help Centre'}
            <HelpCentre {token} role={user?.role} />

          {:else if selectedPage === 'Profile'}
            <Profile {token} {user} />

          {:else}
            <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
              <h3 class="text-base font-semibold text-white">Page not found</h3>
              <p class="mt-2 text-sm text-slate-400">The page you are looking for does not exist.</p>
            </section>
          {/if}

        </main>
      </div>
    </div>
  {/if}
</div>
