<script>
  import { fetchFcaCompany, fetchNetworks, registerCompany } from '../lib/auth.js';

  let { onback = null, onsuccess = null, type = 'Broker', enforceAuthorised = false } = $props();

  const typeLabel = { Broker: 'company', Network: 'network', Club: 'club' };
  const label = $derived(typeLabel[type] ?? 'company');

  let compName       = $state('');
  let compFCA        = $state('');
  let compPhone      = $state('');
  let compEmail      = $state('');
  let compWebsite    = $state('');
  let compFcaStatus  = $state('');
  let tradingNames   = $state([]);
  let fcaAddresses   = $state([]);

  let fcaLoading    = $state(false);
  let fcaStatus     = $state('');
  let fcaError      = $state('');
  let formError     = $state('');
  let saving        = $state(false);

  let networks        = $state([]);
  let networksLoading = $state(false);
  let selectedNetwork = $state(null);

  const isAppointedRep = $derived(
    compFcaStatus.toLowerCase().includes('appointed representative')
  );

  async function lookupFca() {
    fcaError   = '';
    fcaStatus  = '';
    fcaAddresses = [];
    if (!compFCA.trim()) { fcaError = 'Enter an FCA reference number first.'; return; }

    fcaLoading = true;
    const result = await fetchFcaCompany(compFCA.trim());
    fcaLoading = false;

    if (result.error) { fcaError = result.error; return; }

    if (enforceAuthorised && result.status?.toLowerCase() !== 'authorised') {
      fcaError = `This firm is not Authorised on the FCA Register (status: ${result.status ?? 'unknown'}). Only Authorised firms can be registered as networks.`;
      return;
    }

    if (result.name)   compName      = result.name;
    if (result.status) compFcaStatus = result.status;
    if (result.phone)   compPhone    = result.phone;
    if (result.email)   compEmail    = result.email;
    if (result.website) compWebsite  = result.website;

    if (result.tradingNames?.length > 0) tradingNames = result.tradingNames;

    if (result.addresses?.length > 0) fcaAddresses = result.addresses;

    if (result.status?.toLowerCase().includes('appointed representative')) {
      selectedNetwork = null;
      networksLoading = true;
      const nets = await fetchNetworks();
      networksLoading = false;
      networks = Array.isArray(nets) ? nets : [];
    } else {
      networks = [];
      selectedNetwork = null;
    }

    fcaStatus = result.status ? `${result.name} — ${result.status}` : result.name;
  }

  async function submit() {
    formError = '';
    if (!compName.trim())  { formError = 'Company name is required.'; return; }
    if (!compFCA.trim())   { formError = 'FCA reference number is required.'; return; }
    if (!compEmail.trim()) { formError = 'Company email address is required.'; return; }
    if (isAppointedRep && !selectedNetwork) {
      formError = 'You must select a network before registering an Appointed Representative.';
      return;
    }

    saving = true;
    const result = await registerCompany({
      name:            compName.trim(),
      fcaNumber:       compFCA.trim(),
      phone:           compPhone   || null,
      email:           compEmail   || null,
      website:         compWebsite || null,
      fcaStatus:       compFcaStatus || null,
      parentCompanyId: selectedNetwork?.id ?? null,
      type,
      tradingNames: tradingNames.map(t => ({
        name:          t.name,
        status:        t.status        || null,
        effectiveFrom: t.effectiveFrom || null,
        effectiveTo:   null
      })),
      addresses: fcaAddresses.map(a => ({
        type:                            a.addressType                     || 'Registered',
        organisationName:                null,
        departmentName:                  null,
        subBuildingName:                 null,
        buildingName:                    null,
        buildingNumber:                  a.buildingNumber                  || null,
        dependentThoroughfareName:       null,
        dependentThoroughfareDescriptor: null,
        thoroughfareName:                a.thoroughfareName                || null,
        thoroughfareDescriptor:          null,
        doubleDependentLocality:         null,
        dependentLocality:               a.dependentLocality               || null,
        postTown:                        a.postTown                        || null,
        postcode:                        a.postcode                        || null,
        poBox:                           null,
        country:                         a.country                        || null
      }))
    });
    saving = false;

    if (result.error) { formError = result.error; return; }
    onsuccess?.(result);
  }

  function formatAddress(a) {
    return [
      a.buildingNumber && a.thoroughfareName
        ? `${a.buildingNumber} ${a.thoroughfareName}`
        : (a.thoroughfareName ?? ''),
      a.dependentLocality,
      a.postTown,
      a.postcode,
      a.country
    ].filter(Boolean).join(', ');
  }

  const addressTypeColour = {
    'Registered':    'bg-sky-500/10 text-sky-400',
    'Trading':       'bg-violet-500/10 text-violet-400',
    'Correspondence':'bg-amber-500/10 text-amber-400',
    'Branch':        'bg-emerald-500/10 text-emerald-400',
    'Complaints':    'bg-red-500/10 text-red-400'
  };
</script>

<div class="space-y-5">

  <!-- Header -->
  <div>
    <button
      type="button"
      onclick={onback}
      class="mb-4 flex items-center gap-1.5 text-xs font-medium text-slate-400 transition hover:text-white"
    >
      <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd"/>
      </svg>
      Back
    </button>
    <h3 class="text-lg font-semibold text-white capitalize">Register {label}</h3>
    <p class="mt-1 text-sm text-slate-400">
      Enter the FCA number to auto-fill the details, then review and submit.
      {#if enforceAuthorised}<span class="text-amber-400"> Only Authorised firms may be registered as networks.</span>{/if}
    </p>
  </div>

  {#if formError}
    <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{formError}</p>
  {/if}

  <!-- FCA lookup -->
  <div class="space-y-2">
    <label class="block text-sm font-medium text-slate-300">FCA reference number *</label>
    <div class="flex gap-2">
      <input
        type="text"
        class="flex-1 rounded-xl border border-slate-800 bg-slate-900 px-3 py-2.5 text-sm text-slate-100 outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20"
        bind:value={compFCA}
        placeholder="e.g. 482398"
        onkeydown={(e) => e.key === 'Enter' && lookupFca()}
      />
      <button
        type="button"
        onclick={lookupFca}
        disabled={!compFCA.trim() || fcaLoading}
        class="inline-flex shrink-0 items-center gap-1.5 rounded-xl bg-sky-500/10 px-4 py-2.5 text-sm font-semibold text-sky-400 transition hover:bg-sky-500/20 disabled:opacity-40"
      >
        {#if fcaLoading}
          <svg class="h-3.5 w-3.5 animate-spin" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <path d="M12 2v4m0 12v4M4.93 4.93l2.83 2.83m8.48 8.48 2.83 2.83M2 12h4m12 0h4M4.93 19.07l2.83-2.83m8.48-8.48 2.83-2.83"/>
          </svg>
          Looking up…
        {:else}
          <svg class="h-3.5 w-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
          </svg>
          FCA Lookup
        {/if}
      </button>
    </div>
    {#if fcaError}
      <p class="text-xs text-red-400">{fcaError}</p>
    {:else if fcaStatus}
      <p class="flex items-center gap-1.5 text-xs text-emerald-400">
        <svg class="h-3.5 w-3.5 shrink-0" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" clip-rule="evenodd"/>
        </svg>
        {fcaStatus}
      </p>
    {/if}
  </div>

  <!-- Company details -->
  <div class="space-y-3">
    <div>
      <label class="block text-sm font-medium text-slate-300">Company name *</label>
      <input type="text" class="mt-1.5 w-full rounded-xl border border-slate-800 bg-slate-900 px-3 py-2.5 text-sm text-slate-100 outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20" bind:value={compName} placeholder="Acme Mortgages Ltd" />
    </div>

    <!-- FCA status display -->
    <div>
      <label class="block text-sm font-medium text-slate-300">FCA status</label>
      {#if compFcaStatus}
        <div class="mt-1.5 flex items-center gap-2 rounded-xl border border-slate-700/60 bg-slate-900/60 px-3 py-2.5">
          <span class="inline-block h-2 w-2 shrink-0 rounded-full {compFcaStatus.toLowerCase() === 'authorised' ? 'bg-emerald-400' : 'bg-amber-400'}"></span>
          <span class="text-sm text-slate-200">{compFcaStatus}</span>
        </div>
      {:else}
        <div class="mt-1.5 rounded-xl border border-dashed border-slate-700/60 px-3 py-2.5 text-sm text-slate-500">
          Look up FCA number to populate
        </div>
      {/if}
    </div>

    <!-- Appointed Representative — network selector -->
    {#if isAppointedRep}
      <div class="space-y-2 rounded-2xl border border-amber-500/30 bg-amber-500/5 p-4">
        <div class="flex items-start gap-2">
          <svg class="mt-0.5 h-4 w-4 shrink-0 text-amber-400" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd"/>
          </svg>
          <p class="text-sm font-medium text-amber-300">Appointed Representative — network required</p>
        </div>
        {#if networksLoading}
          <p class="text-sm text-slate-400">Loading registered networks…</p>
        {:else if networks.length === 0}
          <p class="mt-1 text-sm text-slate-300">
            No networks are currently registered on this platform. Please ask your network to
            <a href="mailto:support@example.com" class="font-medium text-sky-400 underline underline-offset-2 hover:text-sky-300">get in touch</a>
            to register before completing your application.
          </p>
        {:else}
          <div>
            <label for="network-select" class="block text-xs font-medium text-slate-400">Select your network *</label>
            <select
              id="network-select"
              class="mt-1 w-full rounded-xl border border-slate-700 bg-slate-900/80 px-3 py-2.5 text-sm text-slate-100 outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20"
              onchange={(e) => {
                const id = parseInt(e.target.value, 10);
                selectedNetwork = networks.find(n => n.id === id) ?? null;
              }}
            >
              <option value="">— Select a network —</option>
              {#each networks as net}
                <option value={net.id}>{net.name} ({net.fcaNumber})</option>
              {/each}
            </select>
          </div>
        {/if}
      </div>
    {/if}

    <div class="grid grid-cols-2 gap-3">
      <div>
        <label class="block text-sm font-medium text-slate-300">Phone</label>
        <input type="tel" class="mt-1.5 w-full rounded-xl border border-slate-800 bg-slate-900 px-3 py-2.5 text-sm text-slate-100 outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20" bind:value={compPhone} placeholder="+44 20 0000 0000" />
      </div>
      <div>
        <label class="block text-sm font-medium text-slate-300">Email *</label>
        <input type="email" class="mt-1.5 w-full rounded-xl border border-slate-800 bg-slate-900 px-3 py-2.5 text-sm text-slate-100 outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20" bind:value={compEmail} placeholder="info@company.com" />
      </div>
    </div>
    <div>
      <label class="block text-sm font-medium text-slate-300">Website</label>
      <input type="text" class="mt-1.5 w-full rounded-xl border border-slate-800 bg-slate-900 px-3 py-2.5 text-sm text-slate-100 outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20" bind:value={compWebsite} placeholder="https://company.com" />
    </div>
  </div>

  <!-- Trading names (read-only, imported from FCA) -->
  {#if tradingNames.length > 0}
    <div class="space-y-2 rounded-2xl border border-slate-700/60 bg-slate-950/50 p-4">
      <div class="flex items-center justify-between">
        <p class="text-xs font-semibold uppercase tracking-wider text-slate-400">Trading names</p>
        <span class="rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-400">Imported from FCA</span>
      </div>
      <div class="mt-1 divide-y divide-slate-800">
        {#each tradingNames as tn}
          <div class="flex items-center justify-between py-2">
            <div>
              <p class="text-sm text-slate-200">{tn.name}</p>
              {#if tn.effectiveFrom}
                <p class="text-xs text-slate-500">From {tn.effectiveFrom}</p>
              {/if}
            </div>
            {#if tn.status}
              <span class="rounded-full bg-slate-800 px-2 py-0.5 text-xs text-slate-400">{tn.status}</span>
            {/if}
          </div>
        {/each}
      </div>
    </div>
  {/if}

  <!-- Addresses (read-only, imported from FCA) -->
  {#if fcaAddresses.length > 0}
    <div class="space-y-2 rounded-2xl border border-slate-700/60 bg-slate-950/50 p-4">
      <div class="flex items-center justify-between">
        <p class="text-xs font-semibold uppercase tracking-wider text-slate-400">
          Addresses
          <span class="ml-1.5 rounded-full bg-slate-800 px-2 py-0.5 text-slate-500">{fcaAddresses.length}</span>
        </p>
        <span class="rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-400">Imported from FCA</span>
      </div>
      <div class="mt-1 divide-y divide-slate-800">
        {#each fcaAddresses as a}
          <div class="flex items-start gap-3 py-3">
            <svg class="mt-0.5 h-4 w-4 shrink-0 text-slate-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">
              <path d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
              <path d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
            </svg>
            <div class="flex-1">
              <p class="text-sm text-slate-200">{formatAddress(a)}</p>
              {#if a.fcaType}
                <p class="mt-0.5 text-xs text-slate-500">{a.fcaType}</p>
              {/if}
            </div>
            <span class="shrink-0 rounded-full px-2 py-0.5 text-xs font-medium {addressTypeColour[a.addressType] ?? 'bg-slate-800 text-slate-400'}">
              {a.addressType}
            </span>
          </div>
        {/each}
      </div>
    </div>
  {:else if fcaStatus}
    <!-- Looked up but no addresses returned -->
    <div class="rounded-2xl border border-slate-700/60 bg-slate-950/50 p-4">
      <p class="text-xs font-semibold uppercase tracking-wider text-slate-400">Addresses</p>
      <p class="mt-2 text-sm text-slate-500">No addresses were returned by the FCA Register for this firm.</p>
    </div>
  {/if}

  <button
    type="button"
    onclick={submit}
    disabled={saving || !fcaStatus}
    class="w-full rounded-2xl bg-sky-500 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 transition hover:bg-sky-400 disabled:opacity-50"
    title={!fcaStatus ? 'Complete an FCA lookup first' : ''}
  >
    {saving ? 'Registering…' : `Register ${label}`}
  </button>
  {#if !fcaStatus}
    <p class="text-center text-xs text-slate-500">Complete an FCA lookup before registering</p>
  {/if}
</div>
