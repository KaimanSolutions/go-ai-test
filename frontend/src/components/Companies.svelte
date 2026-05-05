<script>
  import { onMount } from 'svelte';
  import { fetchCompanies, fetchCompanyDetail, addCompanyBankDetails, deleteCompanyBankDetails } from '../lib/auth.js';
  import CompanyRegister from './CompanyRegister.svelte';

  let companies = $state([]);
  let loading = $state(true);
  let activeTab = $state('Broker');
  let selected = $state(null);
  let loadingDetail = $state(false);
  let showRegister = $state(false);
  let error = $state('');

  let showAddBank   = $state(false);
  let bankSaving    = $state(false);
  let bankError     = $state('');
  let bank = $state({ accountName: '', bankName: '', accountNumber: '', sortCode: '' });

  const tabs = [
    { key: 'Broker',  label: 'Companies' },
    { key: 'Network', label: 'Networks'  },
    { key: 'Club',    label: 'Clubs'     }
  ];

  let filtered = $derived(companies.filter(c => c.type === activeTab));

  onMount(async () => {
    const result = await fetchCompanies();
    loading = false;
    if (result.error) { error = result.error; return; }
    companies = result;
  });

  async function openCompany(id) {
    selected = null;
    loadingDetail = true;
    const result = await fetchCompanyDetail(id);
    loadingDetail = false;
    if (result.error) { error = result.error; return; }
    selected = result;
  }

  function back() {
    selected = null;
    error = '';
  }

  async function handleRegistered(company) {
    showRegister = false;
    const result = await fetchCompanies();
    if (!result.error) companies = result;
    await openCompany(company.id);
  }

  function countOf(type) {
    return companies.filter(c => c.type === type).length;
  }

  const typeBadge = {
    Broker:  'bg-sky-500/15 text-sky-400',
    Network: 'bg-violet-500/15 text-violet-400',
    Club:    'bg-emerald-500/15 text-emerald-400'
  };

  const addrTypeBadge = {
    Registered:     'bg-slate-700 text-slate-300',
    Trading:        'bg-sky-500/15 text-sky-400',
    Correspondence: 'bg-amber-500/15 text-amber-400',
    Branch:         'bg-violet-500/15 text-violet-400',
    Complaints:     'bg-red-500/15 text-red-400'
  };

  function formatSortCode(raw) {
    const digits = raw.replace(/\D/g, '').slice(0, 6);
    return digits.replace(/(\d{2})(\d{2})?(\d{2})?/, (_, a, b, c) =>
      [a, b, c].filter(Boolean).join('-')
    );
  }

  async function submitBankDetails() {
    bankError = '';
    if (!bank.accountName.trim()) { bankError = 'Account name is required.'; return; }
    if (!bank.bankName.trim())    { bankError = 'Bank name is required.'; return; }
    if (!bank.accountNumber.trim()) { bankError = 'Account number is required.'; return; }
    if (!bank.sortCode.trim())    { bankError = 'Sort code is required.'; return; }

    bankSaving = true;
    const result = await addCompanyBankDetails(selected.id, {
      accountName:   bank.accountName.trim(),
      bankName:      bank.bankName.trim(),
      accountNumber: bank.accountNumber.trim(),
      sortCode:      bank.sortCode.trim()
    });
    bankSaving = false;

    if (result.error) { bankError = result.error; return; }
    bank = { accountName: '', bankName: '', accountNumber: '', sortCode: '' };
    showAddBank = false;
    const refreshed = await fetchCompanyDetail(selected.id);
    if (!refreshed.error) selected = refreshed;
  }

  async function removeBankDetails(bankDetailsId) {
    const result = await deleteCompanyBankDetails(selected.id, bankDetailsId);
    if (result.error) { error = result.error; return; }
    const refreshed = await fetchCompanyDetail(selected.id);
    if (!refreshed.error) selected = refreshed;
  }

  function formatAddress(a) {
    return [
      a.subBuildingName,
      a.buildingNumber && a.thoroughfareName
        ? `${a.buildingNumber} ${a.thoroughfareName}`
        : (a.buildingName || a.thoroughfareName || null),
      a.dependentLocality,
      a.postTown,
      a.postcode,
      a.country
    ].filter(Boolean);
  }
</script>

{#if showRegister}
  <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
    <CompanyRegister
      type={activeTab}
      enforceAuthorised={activeTab === 'Network'}
      onback={() => showRegister = false}
      onsuccess={handleRegistered}
    />
  </div>

{:else}

{#if error}
  <div class="mb-4 rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</div>
{/if}

{#if loadingDetail}
  <div class="flex items-center justify-center py-24">
    <div class="h-8 w-8 animate-spin rounded-full border-2 border-slate-700 border-t-sky-500"></div>
  </div>

{:else if selected}
  <!-- Detail view -->
  <div class="space-y-6">
    <button
      onclick={back}
      class="flex items-center gap-1.5 text-sm font-medium text-slate-400 transition hover:text-white"
    >
      <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd"/>
      </svg>
      Back to list
    </button>

    <!-- Header card -->
    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <span class="inline-block rounded-full px-2.5 py-0.5 text-xs font-semibold {typeBadge[selected.type] ?? 'bg-slate-800 text-slate-400'}">
            {selected.type}
          </span>
          <h2 class="mt-2 text-2xl font-semibold text-white">{selected.name}</h2>
          <div class="mt-1 flex flex-wrap items-center gap-2">
            <p class="text-sm text-slate-400">FCA: {selected.fcaNumber}</p>
            {#if selected.fcaStatus}
              <span class="rounded-full px-2 py-0.5 text-xs font-medium
                {selected.fcaStatus.toLowerCase() === 'authorised' ? 'bg-emerald-500/15 text-emerald-400' :
                 selected.fcaStatus.toLowerCase() === 'registered' ? 'bg-sky-500/15 text-sky-400' :
                 'bg-amber-500/15 text-amber-400'}">
                {selected.fcaStatus}
              </span>
            {/if}
          </div>
        </div>
        <p class="text-xs text-slate-500">Registered {new Date(selected.createdAt).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}</p>
      </div>

      <div class="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {#if selected.address}
          <div>
            <p class="text-xs font-medium uppercase tracking-wider text-slate-500">Address</p>
            <p class="mt-1 text-sm text-slate-200">{selected.address}</p>
          </div>
        {/if}
        {#if selected.phone}
          <div>
            <p class="text-xs font-medium uppercase tracking-wider text-slate-500">Phone</p>
            <p class="mt-1 text-sm text-slate-200">{selected.phone}</p>
          </div>
        {/if}
        {#if selected.email}
          <div>
            <p class="text-xs font-medium uppercase tracking-wider text-slate-500">Email</p>
            <a href="mailto:{selected.email}" class="mt-1 block text-sm text-sky-400 hover:underline">{selected.email}</a>
          </div>
        {/if}
        {#if selected.website}
          <div>
            <p class="text-xs font-medium uppercase tracking-wider text-slate-500">Website</p>
            <a href={selected.website} target="_blank" rel="noopener noreferrer" class="mt-1 block text-sm text-sky-400 hover:underline truncate">{selected.website}</a>
          </div>
        {/if}
      </div>
    </div>

    <!-- Trading names -->
    {#if selected.tradingNames?.length > 0}
      <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
        <h3 class="text-base font-semibold text-white">
          Trading names
          <span class="ml-1.5 rounded-full bg-slate-800 px-2 py-0.5 text-xs text-slate-400">{selected.tradingNames.length}</span>
        </h3>
        <div class="mt-4 overflow-hidden rounded-2xl border border-slate-800">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-slate-800 bg-slate-950/60">
                <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Name</th>
                <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Status</th>
                <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Effective from</th>
                <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Effective to</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-800">
              {#each selected.tradingNames as tn}
                <tr class="hover:bg-slate-800/40 transition">
                  <td class="px-4 py-3 font-medium text-white">{tn.name}</td>
                  <td class="px-4 py-3">
                    {#if tn.status}
                      <span class="rounded-full bg-slate-800 px-2 py-0.5 text-xs text-slate-400">{tn.status}</span>
                    {:else}
                      <span class="text-slate-600">—</span>
                    {/if}
                  </td>
                  <td class="px-4 py-3 text-slate-400">{tn.effectiveFrom ?? '—'}</td>
                  <td class="px-4 py-3 text-slate-400">{tn.effectiveTo ?? '—'}</td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      </div>
    {/if}

    <!-- Addresses -->
    {#if selected.addresses?.length > 0}
      <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
        <h3 class="text-base font-semibold text-white">
          Addresses
          <span class="ml-1.5 rounded-full bg-slate-800 px-2 py-0.5 text-xs text-slate-400">{selected.addresses.length}</span>
        </h3>
        <div class="mt-4 grid gap-4 sm:grid-cols-2">
          {#each selected.addresses as addr}
            <div class="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
              <span class="inline-block rounded-full px-2 py-0.5 text-xs font-medium {addrTypeBadge[addr.type] ?? 'bg-slate-800 text-slate-400'}">
                {addr.type}
              </span>
              <address class="mt-3 space-y-0.5 not-italic text-sm text-slate-300">
                {#each formatAddress(addr) as line}
                  <p>{line}</p>
                {/each}
              </address>
              {#if addr.poBox}
                <p class="mt-1 text-xs text-slate-500">PO Box {addr.poBox}</p>
              {/if}
            </div>
          {/each}
        </div>
      </div>
    {/if}

    <!-- Bank details -->
    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <svg class="h-4 w-4 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
            <path stroke-linecap="round" stroke-linejoin="round" d="M3 6l9-3 9 3v2H3V6zm0 0v12a1 1 0 001 1h16a1 1 0 001-1V6M3 10h18M3 14h18"/>
          </svg>
          <h3 class="text-base font-semibold text-white">
            Bank accounts
            {#if selected.bankAccounts?.length > 0}
              <span class="ml-1.5 rounded-full bg-slate-800 px-2 py-0.5 text-xs text-slate-400">{selected.bankAccounts.length}</span>
            {/if}
          </h3>
        </div>
        <button
          onclick={() => { showAddBank = !showAddBank; bankError = ''; }}
          class="inline-flex items-center gap-1.5 rounded-xl bg-slate-800 px-3 py-1.5 text-xs font-medium text-slate-300 transition hover:bg-slate-700"
        >
          <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
            <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z"/>
          </svg>
          Add account
        </button>
      </div>

      {#if showAddBank}
        <div class="mt-4 space-y-3 rounded-2xl border border-slate-700/60 bg-slate-950/60 p-4">
          {#if bankError}
            <p class="rounded-xl bg-red-500/10 px-3 py-2 text-xs font-medium text-red-400">{bankError}</p>
          {/if}
          <div class="grid gap-3 sm:grid-cols-2">
            <div>
              <label for="bank-account-name" class="block text-xs font-medium text-slate-400">Account name *</label>
              <input id="bank-account-name" type="text" bind:value={bank.accountName} placeholder="Acme Mortgages Ltd"
                class="mt-1 w-full rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20" />
            </div>
            <div>
              <label for="bank-name" class="block text-xs font-medium text-slate-400">Bank name *</label>
              <input id="bank-name" type="text" bind:value={bank.bankName} placeholder="Barclays"
                class="mt-1 w-full rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20" />
            </div>
            <div>
              <label for="bank-account-number" class="block text-xs font-medium text-slate-400">Account number *</label>
              <input id="bank-account-number" type="text" bind:value={bank.accountNumber} placeholder="12345678" maxlength="8"
                class="mt-1 w-full rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20 font-mono" />
            </div>
            <div>
              <label for="bank-sort-code" class="block text-xs font-medium text-slate-400">Sort code *</label>
              <input id="bank-sort-code" type="text"
                value={formatSortCode(bank.sortCode)}
                oninput={(e) => bank.sortCode = e.target.value.replace(/\D/g, '').slice(0, 6)}
                placeholder="12-34-56" maxlength="8"
                class="mt-1 w-full rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20 font-mono" />
            </div>
          </div>
          <div class="flex justify-end gap-2 pt-1">
            <button onclick={() => { showAddBank = false; bankError = ''; }}
              class="rounded-xl px-3 py-2 text-xs font-medium text-slate-400 transition hover:text-white">
              Cancel
            </button>
            <button onclick={submitBankDetails} disabled={bankSaving}
              class="rounded-xl bg-sky-500 px-4 py-2 text-xs font-semibold text-white transition hover:bg-sky-400 disabled:opacity-50">
              {bankSaving ? 'Saving…' : 'Save account'}
            </button>
          </div>
        </div>
      {/if}

      {#if selected.bankAccounts?.length > 0}
        <div class="mt-4 space-y-2">
          {#each selected.bankAccounts as acct}
            <div class="flex items-center justify-between rounded-2xl border border-slate-800 bg-slate-950/60 px-4 py-3">
              <div class="grid gap-x-6 gap-y-0.5 sm:grid-cols-4">
                <div>
                  <p class="text-xs font-medium uppercase tracking-wider text-slate-500">Account name</p>
                  <p class="mt-0.5 text-sm text-slate-200">{acct.accountName}</p>
                </div>
                <div>
                  <p class="text-xs font-medium uppercase tracking-wider text-slate-500">Bank</p>
                  <p class="mt-0.5 text-sm text-slate-200">{acct.bankName}</p>
                </div>
                <div>
                  <p class="text-xs font-medium uppercase tracking-wider text-slate-500">Account number</p>
                  <p class="mt-0.5 font-mono text-sm text-slate-200">****{acct.accountNumber.slice(-4)}</p>
                </div>
                <div>
                  <p class="text-xs font-medium uppercase tracking-wider text-slate-500">Sort code</p>
                  <p class="mt-0.5 font-mono text-sm text-slate-200">
                    {acct.sortCode.replace(/(\d{2})(\d{2})(\d{2})/, '$1-$2-$3')}
                  </p>
                </div>
              </div>
              <button
                onclick={() => removeBankDetails(acct.id)}
                class="ml-4 shrink-0 rounded-lg p-1.5 text-slate-600 transition hover:bg-red-500/10 hover:text-red-400"
                title="Remove bank account"
              >
                <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.584.176-2.365.298a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.52.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193v-.443A2.75 2.75 0 0011.25 1h-2.5zm0 1.5h2.5c.69 0 1.25.56 1.25 1.25v.37a49.12 49.12 0 00-5 0v-.37c0-.69.56-1.25 1.25-1.25zm-1.36 5.31a.75.75 0 011.498.07l-.3 7.5a.75.75 0 11-1.498-.07l.3-7.5zm4.11.07a.75.75 0 10-1.498.07l.3 7.5a.75.75 0 001.498-.07l-.3-7.5z" clip-rule="evenodd"/>
                </svg>
              </button>
            </div>
          {/each}
        </div>
      {:else if !showAddBank}
        <p class="mt-4 text-sm text-slate-500">No bank accounts added yet.</p>
      {/if}
    </div>

    <!-- Linked network -->
    {#if selected.parentCompany}
      <div class="rounded-3xl border border-violet-500/20 bg-slate-900/95 p-6 shadow-xl">
        <div class="flex items-center gap-2">
          <svg class="h-4 w-4 text-violet-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
            <path stroke-linecap="round" stroke-linejoin="round" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"/>
          </svg>
          <h3 class="text-base font-semibold text-white">Linked network</h3>
        </div>

        <div class="mt-4 flex items-start justify-between gap-4 rounded-2xl border border-violet-500/20 bg-violet-500/5 p-4">
          <div class="space-y-3 min-w-0 flex-1">
            <div>
              <p class="font-semibold text-white">{selected.parentCompany.name}</p>
              <p class="mt-0.5 text-xs text-slate-400">FCA: {selected.parentCompany.fcaNumber}</p>
            </div>
            <div class="grid gap-3 sm:grid-cols-3">
              {#if selected.parentCompany.email}
                <div>
                  <p class="text-xs font-medium uppercase tracking-wider text-slate-500">Email</p>
                  <a href="mailto:{selected.parentCompany.email}" class="mt-0.5 block truncate text-sm text-sky-400 hover:underline">
                    {selected.parentCompany.email}
                  </a>
                </div>
              {/if}
              {#if selected.parentCompany.phone}
                <div>
                  <p class="text-xs font-medium uppercase tracking-wider text-slate-500">Phone</p>
                  <p class="mt-0.5 text-sm text-slate-200">{selected.parentCompany.phone}</p>
                </div>
              {/if}
              {#if selected.parentCompany.website}
                <div>
                  <p class="text-xs font-medium uppercase tracking-wider text-slate-500">Website</p>
                  <a href={selected.parentCompany.website} target="_blank" rel="noopener noreferrer" class="mt-0.5 block truncate text-sm text-sky-400 hover:underline">
                    {selected.parentCompany.website}
                  </a>
                </div>
              {/if}
            </div>
          </div>
          <button
            onclick={() => openCompany(selected.parentCompany.id)}
            class="shrink-0 rounded-xl border border-slate-700 px-3 py-2 text-xs font-medium text-slate-300 transition hover:bg-slate-800"
          >
            View
          </button>
        </div>
      </div>
    {/if}

    <!-- Child companies -->
    {#if selected.childCompanies?.length > 0}
      <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
        <h3 class="text-base font-semibold text-white">Member companies <span class="ml-1.5 rounded-full bg-slate-800 px-2 py-0.5 text-xs text-slate-400">{selected.childCompanies.length}</span></h3>
        <div class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {#each selected.childCompanies as child}
            <button
              onclick={() => openCompany(child.id)}
              class="flex items-center justify-between rounded-2xl border border-slate-700 px-4 py-3 text-left text-sm transition hover:bg-slate-800"
            >
              <div>
                <p class="font-medium text-white">{child.name}</p>
                <span class="text-xs {typeBadge[child.type] ?? ''}">{child.type}</span>
              </div>
              <svg class="h-4 w-4 shrink-0 text-slate-600" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd"/>
              </svg>
            </button>
          {/each}
        </div>
      </div>
    {/if}

    <!-- Brokers -->
    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <h3 class="text-base font-semibold text-white">
        Brokers
        <span class="ml-1.5 rounded-full bg-slate-800 px-2 py-0.5 text-xs text-slate-400">{selected.brokers?.length ?? 0}</span>
      </h3>
      {#if !selected.brokers?.length}
        <p class="mt-4 text-sm text-slate-500">No brokers registered under this company yet.</p>
      {:else}
        <div class="mt-4 overflow-hidden rounded-2xl border border-slate-800">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-slate-800 bg-slate-950/60">
                <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Name</th>
                <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Email</th>
                <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Phone</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-800">
              {#each selected.brokers as broker}
                <tr class="hover:bg-slate-800/40 transition">
                  <td class="px-4 py-3 font-medium text-white">{broker.firstName} {broker.lastName}</td>
                  <td class="px-4 py-3 text-slate-400">{broker.email}</td>
                  <td class="px-4 py-3 text-slate-400">{broker.phone || '—'}</td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      {/if}
    </div>
  </div>

{:else}
  <!-- List view -->
  <div class="space-y-6">
    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 class="text-base font-semibold text-white">Companies</h3>
          <p class="mt-1 text-sm text-slate-400">View and manage registered companies, networks, and clubs.</p>
        </div>
        <button
          onclick={() => showRegister = true}
          class="inline-flex shrink-0 items-center gap-2 rounded-2xl bg-sky-500 px-4 py-2.5 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 transition hover:bg-sky-400"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z"/>
          </svg>
          Register {activeTab === 'Network' ? 'network' : activeTab === 'Club' ? 'club' : 'company'}
        </button>
      </div>

      <!-- Tabs -->
      <div class="mt-5 flex gap-1 rounded-2xl border border-slate-800 bg-slate-950/60 p-1">
        {#each tabs as tab}
          <button
            onclick={() => activeTab = tab.key}
            class="flex flex-1 items-center justify-center gap-2 rounded-xl py-2 text-sm font-semibold transition
              {activeTab === tab.key ? 'bg-sky-500/10 text-sky-400' : 'text-slate-400 hover:text-white'}"
          >
            {tab.label}
            <span class="rounded-full px-1.5 py-0.5 text-xs
              {activeTab === tab.key ? 'bg-sky-500/20 text-sky-400' : 'bg-slate-800 text-slate-500'}">
              {countOf(tab.key)}
            </span>
          </button>
        {/each}
      </div>

      <!-- Grid -->
      <div class="mt-5">
        {#if loading}
          <div class="flex items-center justify-center py-16">
            <div class="h-7 w-7 animate-spin rounded-full border-2 border-slate-700 border-t-sky-500"></div>
          </div>
        {:else if filtered.length === 0}
          <div class="flex flex-col items-center justify-center gap-3 py-16 text-center">
            <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-800">
              <svg class="h-6 w-6 text-slate-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
                <path stroke-linecap="round" stroke-linejoin="round" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
              </svg>
            </div>
            <p class="text-sm font-medium text-slate-400">No {tabs.find(t => t.key === activeTab)?.label.toLowerCase()} registered yet.</p>
          </div>
        {:else}
          <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {#each filtered as company}
              <button
                onclick={() => openCompany(company.id)}
                class="group flex flex-col items-start gap-4 rounded-3xl border border-slate-800 bg-slate-950/80 p-5 text-left transition hover:border-sky-500/40 hover:bg-slate-800/60"
              >
                <div class="flex w-full items-start justify-between gap-2">
                  <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl {activeTab === 'Broker' ? 'bg-sky-500/15' : activeTab === 'Network' ? 'bg-violet-500/15' : 'bg-emerald-500/15'}">
                    <svg class="h-5 w-5 {activeTab === 'Broker' ? 'text-sky-400' : activeTab === 'Network' ? 'text-violet-400' : 'text-emerald-400'}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
                    </svg>
                  </div>
                  <svg class="h-4 w-4 shrink-0 text-slate-600 transition group-hover:text-slate-400" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd"/>
                  </svg>
                </div>
                <div class="min-w-0 flex-1">
                  <p class="truncate font-semibold text-white group-hover:text-sky-300 transition">{company.name}</p>
                  <p class="mt-0.5 text-xs text-slate-500">FCA: {company.fcaNumber}</p>
                  {#if company.email}
                    <p class="mt-0.5 truncate text-xs text-slate-500">{company.email}</p>
                  {/if}
                </div>
                <div class="flex w-full items-center justify-between border-t border-slate-800 pt-3">
                  <span class="text-xs text-slate-500">{company.brokerCount ?? 0} broker{company.brokerCount !== 1 ? 's' : ''}</span>
                  <span class="rounded-full px-2 py-0.5 text-xs font-medium {typeBadge[company.type] ?? 'bg-slate-800 text-slate-400'}">{company.type}</span>
                </div>
              </button>
            {/each}
          </div>
        {/if}
      </div>
    </div>
  </div>
{/if}

{/if}
