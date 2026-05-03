<script>
  import { onMount } from 'svelte';
  import Auth from './components/Auth.svelte';
  import FormBuilder from './components/FormBuilder.svelte';
  import FormRenderer from './components/FormRenderer.svelte';
  import { login, startSSO, fetchBrandingSettings, saveBrandingSettings, fetchSchemas, fetchSchema, saveSchema, validateSubmission } from './lib/auth.js';
  import Settings from './components/Settings.svelte';
  import HelpCentre from './components/HelpCentre.svelte';

  let token = $state('');
  let user = $state(null);
  let error = $state('');
  let selectedPage = $state('Dashboard');
  let schema = $state(null);
  let validationResult = null;
  let schemas = $state([]);
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
  const menuItems = ['Dashboard', 'Forms', 'Settings', 'Integrations', 'Help Centre'];
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

  async function handleLogin(credentials) {
    error = '';
    const response = await login(credentials.username, credentials.password);
    if (response.error) {
      error = response.error;
      return;
    }

    token = response.accessToken;
    user = { userName: response.userName, email: response.email };
    selectedPage = 'Dashboard';
    await loadSchema();
    await loadSchemas();
  }

  async function handleSSO() {
    await startSSO();
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

  async function handleSave(updatedSchema) {
    const response = await saveSchema(updatedSchema, token);
    if (response.error) {
      error = response.error;
      return;
    }
    schema = response;
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
    selectedPage = 'Settings';
  }

  function editForm(id) {
    selectedFormId = id;
    selectedPage = 'Settings';
    loadSchema();
  }

  function deleteForm(id) {
    schemas = schemas.filter(s => s.id !== id);
  }

  onMount(async () => {
    await loadBranding();
    await loadSchemas();
  });

  let styleVars = $derived(`--brand-primary: ${branding.primaryColor}; --brand-accent: ${branding.accentColor}; --brand-bg: ${branding.backgroundColor}; --brand-surface: ${branding.surfaceColor}; --brand-card: ${branding.cardColor}; --brand-text: ${branding.textColor}; --brand-muted: ${branding.mutedTextColor}; --font-body: ${branding.fontBody}; --font-heading: ${branding.fontHeading};`);
</script>

<style>
  .error {
    color: #dc2626;
    margin: 0.75rem 0;
  }
</style>

<div
  class="min-h-screen bg-brand-bg text-brand-text"
  {styleVars}
>
  <div class="mx-auto flex min-h-screen max-w-7xl flex-col px-4 py-8 sm:px-6 lg:px-8">
    <header class="flex items-center justify-between gap-6 pb-6 text-brand-text">
      <div>
        <h1 class="text-3xl font-semibold tracking-tight sm:text-4xl">Mortgage Originations Platform</h1>
      </div>
      <div class="rounded-3xl border border-slate-800 bg-slate-900/80 px-4 py-3 text-sm text-slate-300 shadow-sm">
        {#if user}
          <span class="font-medium text-slate-100">Signed in as</span> {user.userName}
        {:else}
          Not signed in
        {/if}
      </div>
    </header>

  {#if error}
    <div class="error rounded-lg bg-red-50 p-4 text-sm font-medium text-red-700">{error}</div>
  {/if}

  {#if !token}
    <div class="relative flex flex-1 items-center justify-center overflow-hidden rounded-[2rem] border border-brand-surface bg-brand-surface/80 p-6 shadow-2xl sm:p-10">
      <div class="absolute inset-0 bg-[radial-gradient(circle_at_top_left,_rgba(56,189,248,0.24),_transparent_30%),radial-gradient(circle_at_bottom_right,_rgba(168,85,247,0.18),_transparent_25%)]"></div>
      <div class="relative grid w-full max-w-5xl gap-8 rounded-[1.75rem] bg-slate-950/95 p-6 shadow-xl sm:grid-cols-[1.2fr_0.8fr] sm:p-10">
        <div class="space-y-6">
          <div>
            <h2 class="text-4xl font-semibold tracking-tight text-white sm:text-5xl">Welcome back</h2>
            <p class="mt-4 max-w-xl text-base leading-7 text-slate-300">Sign in to manage mortgage originations, customize underwriter forms, and validate application rules in one place.</p>
          </div>
          <div class="grid gap-4 rounded-[1.5rem] border border-slate-800 bg-slate-900/80 p-5 text-sm text-slate-300 shadow-inner">
            <div class="flex items-start gap-3">
              <div class="mt-1 h-3.5 w-3.5 rounded-full bg-sky-400"></div>
              <div>
                <p class="font-semibold text-slate-100">Enterprise-grade forms</p>
                <p class="mt-1 leading-6 text-slate-400">Build and save adaptive application forms with conditional validation rules.</p>
              </div>
            </div>
            <div class="flex items-start gap-3">
              <div class="mt-1 h-3.5 w-3.5 rounded-full bg-violet-400"></div>
              <div>
                <p class="font-semibold text-slate-100">SSO enabled</p>
                <p class="mt-1 leading-6 text-slate-400">Connect to your identity provider for secure single sign-on access.</p>
              </div>
            </div>
          </div>
        </div>

        <div class="rounded-[1.75rem] border border-slate-800 bg-slate-900/95 p-6 shadow-xl backdrop-blur-xl sm:p-8">
          <div class="mb-6">
            <p class="text-sm uppercase tracking-[0.2em] text-sky-400">Admin access</p>
            <h3 class="mt-3 text-2xl font-semibold text-white">Sign in to your account</h3>
          </div>
          <Auth on:login={event => handleLogin(event.detail)} on:sso={handleSSO} />
        </div>
      </div>
    </div>
  {:else}
    <div class="mt-8 grid gap-6 xl:grid-cols-[240px_1fr]">
      <aside class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
        <div class="mb-8">
          <p class="text-xs uppercase tracking-[0.24em] text-sky-400">Admin menu</p>
          <h2 class="mt-4 text-xl font-semibold text-white">Control panel</h2>
        </div>
        <nav class="space-y-2">
          {#each menuItems as item}
            <button
              class="w-full rounded-2xl px-4 py-3 text-left text-sm font-semibold transition {selectedPage === item ? 'bg-slate-800 text-white shadow-inner' : 'text-slate-400 hover:bg-slate-800 hover:text-white'}"
              onclick={() => selectedPage = item}
            >
              {item}
            </button>
          {/each}
        </nav>
      </aside>

      <main class="space-y-6">
        <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
          <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 class="text-2xl font-semibold text-white">{selectedPage}</h2>
              <p class="mt-2 text-sm text-slate-400">Manage your mortgage originations workflow from one central page.</p>
            </div>
            <div class="rounded-2xl bg-slate-950/80 px-4 py-2 text-sm text-slate-300">Signed in as {user.userName}</div>
          </div>
        </section>

        {#if selectedPage === 'Dashboard'}
          <section class="grid gap-6 xl:grid-cols-3">
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
          </section>
          <section class="grid gap-6 xl:grid-cols-2">
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
                <button class="rounded-2xl bg-sky-500 px-4 py-3 text-sm font-semibold text-white transition hover:bg-sky-400">Create new application</button>
                <button class="rounded-2xl border border-slate-800 px-4 py-3 text-sm font-semibold text-slate-200 transition hover:bg-slate-800">Review pending forms</button>
              </div>
            </div>
          </section>
        {:else if selectedPage === 'Forms'}
          <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
            <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h3 class="text-xl font-semibold text-white">Forms</h3>
                <p class="mt-2 text-sm text-slate-400">Manage your form schemas.</p>
              </div>
              <button class="rounded-2xl bg-sky-500 px-4 py-3 text-sm font-semibold text-white transition hover:bg-sky-400" onclick={createNewForm}>Create New Form</button>
            </div>
            <div class="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {#each schemas as schema}
                <div class="rounded-3xl border border-slate-800 bg-slate-950/90 p-5">
                  <h4 class="text-lg font-semibold text-white">{schema.title}</h4>
                  <p class="mt-2 text-sm text-slate-400">{schema.description}</p>
                  <div class="mt-4 flex gap-2">
                    <button class="rounded-2xl bg-sky-500 px-3 py-2 text-xs font-semibold text-white transition hover:bg-sky-400" onclick={() => editForm(schema.id)}>Edit</button>
                    <button class="rounded-2xl border border-slate-800 px-3 py-2 text-xs font-semibold text-slate-200 transition hover:bg-slate-800" onclick={() => deleteForm(schema.id)}>Delete</button>
                  </div>
                </div>
              {/each}
            </div>
          </section>
        {:else if selectedPage === 'Settings'}
          <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
            <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h3 class="text-xl font-semibold text-white">Branding settings</h3>
                <p class="mt-2 text-sm text-slate-400">Update colors, fonts, and theme options for your platform.</p>
              </div>
              <button class="rounded-2xl bg-sky-500 px-4 py-3 text-sm font-semibold text-white transition hover:bg-sky-400" onclick={saveBranding}>
                Save branding
              </button>
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
            <div class="mt-6 rounded-3xl border border-slate-800 bg-slate-950/90 p-6 shadow-inner">
              <p class="text-lg font-semibold text-brand-primary" style="font-family: var(--font-heading);">Live theme preview</p>
              <p class="mt-2 text-sm text-slate-400">Your current branding values are applied across the platform.</p>
            </div>
            <div class="mt-6">
              <FormBuilder {schema} on:save={event => handleSave(event.detail)} />
            </div>
          </section>
        {:else if selectedPage === 'Integrations'}
          <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
            <h3 class="text-xl font-semibold text-white">Integrations</h3>
            <p class="mt-3 text-sm text-slate-400">Manage your connected services and APIs.</p>
            <div class="mt-6 grid gap-4 sm:grid-cols-2">
              <div class="rounded-3xl border border-slate-800 bg-slate-950/90 p-5 text-slate-300">Credit bureau</div>
              <div class="rounded-3xl border border-slate-800 bg-slate-950/90 p-5 text-slate-300">E-signature</div>
              <div class="rounded-3xl border border-slate-800 bg-slate-950/90 p-5 text-slate-300">Loan pricing engine</div>
            </div>
          </section>
        {:else if selectedPage === 'Help Centre'}
          <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
            <h3 class="text-xl font-semibold text-white">Help Centre</h3>
            <p class="mt-3 text-sm text-slate-400">Find documentation, support, and onboarding resources.</p>
            <div class="mt-6 grid gap-4 sm:grid-cols-2">
              <div class="rounded-3xl border border-slate-800 bg-slate-950/90 p-5 text-slate-300">Documentation and tutorials.</div>
              <div class="rounded-3xl border border-slate-800 bg-slate-950/90 p-5 text-slate-300">Contact support or open a ticket.</div>
            </div>
          </section>
        {:else}
          <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
            <h3 class="text-xl font-semibold text-white">Page Not Found</h3>
            <p class="mt-3 text-sm text-slate-400">The page you are looking for does not exist.</p>
          </section>
        {/if}
      </main>
    </div>
  {/if}
  </div>
</div>
