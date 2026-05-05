<script>
  import { onMount } from 'svelte';
  import { fetchCompanies } from '../lib/auth.js';
  import CompanyRegister from './CompanyRegister.svelte';

  let { portal = 'Admin', onlogin = null, onregister = null, onsso = null } = $props();

  let mode         = $state('login');
  let companyView  = $state(false);   // true = show CompanyRegister page

  // Login fields
  let loginEmail    = $state('');
  let loginPassword = $state('');

  // Register fields
  let regFirstName = $state('');
  let regLastName  = $state('');
  let regEmail     = $state('');
  let regPassword  = $state('');
  let regConfirm   = $state('');
  let regPhone     = $state('');
  let regCompanyId = $state('');

  // Company list
  let companies        = $state([]);
  let loadingCompanies = $state(false);

  let error = $state('');

  onMount(async () => {
    if (portal === 'Broker') await loadCompanies();
  });

  async function loadCompanies() {
    loadingCompanies = true;
    const result = await fetchCompanies();
    loadingCompanies = false;
    if (!result.error) companies = result;
  }

  async function handleCompanySuccess(company) {
    await loadCompanies();
    regCompanyId = String(company.id);
    companyView  = false;
  }

  function submitLogin() {
    error = '';
    if (!loginEmail || !loginPassword) { error = 'All fields are required.'; return; }
    onlogin?.({ username: loginEmail, password: loginPassword });
  }

  function submitRegister() {
    error = '';
    if (!regFirstName || !regLastName || !regEmail || !regPassword) {
      error = 'Please fill in all required fields.'; return;
    }
    if (regPassword.length < 8) { error = 'Password must be at least 8 characters.'; return; }
    if (regPassword !== regConfirm) { error = 'Passwords do not match.'; return; }
    if (portal === 'Broker' && !regCompanyId) { error = 'Please select a company.'; return; }

    onregister?.({
      email:     regEmail,
      password:  regPassword,
      firstName: regFirstName,
      lastName:  regLastName,
      phone:     regPhone,
      role:      portal,
      companyId: portal === 'Broker' ? parseInt(regCompanyId) : null
    });
  }

  const ic = 'mt-1.5 w-full rounded-xl border border-slate-800 bg-slate-900 px-4 py-3 text-slate-100 shadow-sm outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20';
  const lc = 'block text-sm font-medium text-slate-300';
</script>

<div class="space-y-5">

  {#if companyView}
    <!-- ── Company registration page ── -->
    <CompanyRegister
      onback={() => companyView = false}
      onsuccess={handleCompanySuccess}
    />

  {:else}

    {#if portal !== 'Admin'}
      <div class="flex rounded-2xl border border-slate-800 bg-slate-950/60 p-1">
        <button onclick={() => { mode = 'login'; error = ''; }}
          class="flex-1 rounded-xl py-2 text-sm font-semibold transition {mode === 'login' ? 'bg-sky-500 text-white shadow' : 'text-slate-400 hover:text-white'}">
          Sign in
        </button>
        <button onclick={() => { mode = 'register'; error = ''; }}
          class="flex-1 rounded-xl py-2 text-sm font-semibold transition {mode === 'register' ? 'bg-sky-500 text-white shadow' : 'text-slate-400 hover:text-white'}">
          Create account
        </button>
      </div>
    {/if}

    {#if error}
      <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</p>
    {/if}

    {#if mode === 'login' || portal === 'Admin'}
      <!-- ── Login ── -->
      <div class="space-y-4">
        <div>
          <label for="auth-email" class={lc}>{portal === 'Admin' ? 'Username' : 'Email address'}</label>
          <input id="auth-email" type={portal === 'Admin' ? 'text' : 'email'} class={ic}
            bind:value={loginEmail} placeholder={portal === 'Admin' ? 'admin' : 'you@example.com'} />
        </div>
        <div>
          <label for="auth-pass" class={lc}>Password</label>
          <input id="auth-pass" type="password" class={ic} bind:value={loginPassword}
            placeholder={portal === 'Admin' ? 'Password123!' : '••••••••'}
            onkeydown={(e) => e.key === 'Enter' && submitLogin()} />
        </div>
        <button onclick={submitLogin}
          class="w-full rounded-2xl bg-sky-500 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 transition hover:bg-sky-400">
          Sign in
        </button>

        {#if portal === 'Admin'}
          <div class="relative my-1">
            <div class="absolute inset-0 flex items-center"><div class="w-full border-t border-slate-800"></div></div>
            <div class="relative flex justify-center text-xs"><span class="bg-slate-950 px-2 text-slate-500">Or continue with</span></div>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <button type="button" onclick={() => onsso?.({ provider: 'microsoft' })}
              class="inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-sm font-semibold text-slate-200 transition hover:bg-slate-800">
              <svg class="h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
                <path d="M11.4 24H0V12.6h11.4V24zM24 24H12.6V12.6H24V24zM11.4 11.4H0V0h11.4v11.4zm12.6 0H12.6V0H24v11.4z"/>
              </svg>
              Microsoft
            </button>
            <button type="button" onclick={() => onsso?.({ provider: 'google' })}
              class="inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-sm font-semibold text-slate-200 transition hover:bg-slate-800">
              <svg class="h-4 w-4" viewBox="0 0 24 24">
                <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
                <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
                <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/>
                <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
              </svg>
              Google
            </button>
          </div>
        {/if}
      </div>

    {:else}
      <!-- ── Registration ── -->
      <div class="space-y-4">
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label for="reg-first" class={lc}>First name *</label>
            <input id="reg-first" type="text" class={ic} bind:value={regFirstName} placeholder="Jane" />
          </div>
          <div>
            <label for="reg-last" class={lc}>Last name *</label>
            <input id="reg-last" type="text" class={ic} bind:value={regLastName} placeholder="Smith" />
          </div>
        </div>
        <div>
          <label for="reg-email" class={lc}>Email address *</label>
          <input id="reg-email" type="email" class={ic} bind:value={regEmail} placeholder="you@example.com" />
        </div>
        <div>
          <label for="reg-phone" class={lc}>Phone number</label>
          <input id="reg-phone" type="tel" class={ic} bind:value={regPhone} placeholder="+44 7700 000000" />
        </div>

        {#if portal === 'Broker'}
          <div>
            <label for="reg-company" class={lc}>Company *</label>
            {#if loadingCompanies}
              <p class="mt-2 text-sm text-slate-500">Loading companies…</p>
            {:else}
              <select id="reg-company" class={ic} bind:value={regCompanyId}>
                <option value="">Select a company…</option>
                {#each companies as c}
                  <option value={String(c.id)}>{c.name} — {c.fcaNumber}</option>
                {/each}
              </select>
              <button type="button"
                onclick={() => companyView = true}
                class="mt-2 text-xs font-medium text-sky-400 transition hover:text-sky-300">
                + Register a new company
              </button>
            {/if}
          </div>
        {/if}

        <div>
          <label for="reg-password" class={lc}>Password * <span class="text-slate-500">(min 8 characters)</span></label>
          <input id="reg-password" type="password" class={ic} bind:value={regPassword} placeholder="••••••••" />
        </div>
        <div>
          <label for="reg-confirm" class={lc}>Confirm password *</label>
          <input id="reg-confirm" type="password" class={ic} bind:value={regConfirm} placeholder="••••••••" />
        </div>

        <button onclick={submitRegister}
          class="w-full rounded-2xl bg-sky-500 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 transition hover:bg-sky-400">
          Create {portal === 'Broker' ? 'broker' : 'client'} account
        </button>
      </div>
    {/if}

  {/if}

</div>
