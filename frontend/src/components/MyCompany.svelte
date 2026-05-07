<script>
  import { onMount } from 'svelte';
  import { fetchMyCompany } from '../lib/auth.js';

  let { token } = $props();

  let company = $state(null);
  let loading = $state(true);
  let error   = $state('');

  onMount(load);

  async function load() {
    loading = true;
    error   = '';
    const res = await fetchMyCompany(token);
    loading = false;
    if (res.error) { error = res.error; return; }
    company = res;
  }

  function formatDate(d) {
    return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  function formatAddress(a) {
    return [
      a.organisationName,
      a.departmentName,
      [a.subBuildingName, a.buildingName, a.buildingNumber].filter(Boolean).join(' '),
      [a.dependentThoroughfareName, a.dependentThoroughfareDescriptor].filter(Boolean).join(' '),
      [a.thoroughfareName, a.thoroughfareDescriptor].filter(Boolean).join(' '),
      a.dependentLocality,
      a.doubleDependentLocality,
      a.postTown,
      a.postcode,
      a.country
    ].filter(Boolean).join(', ');
  }
</script>

{#if loading}
  <div class="flex items-center justify-center py-24">
    <div class="h-8 w-8 animate-spin rounded-full border-2 border-slate-700 border-t-violet-500"></div>
  </div>

{:else if error}
  <div class="flex flex-col items-center justify-center rounded-3xl border border-slate-800 bg-slate-900/95 py-16 text-center">
    <div class="flex h-14 w-14 items-center justify-center rounded-full bg-slate-800">
      <svg class="h-6 w-6 text-slate-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
        <path stroke-linecap="round" stroke-linejoin="round" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
      </svg>
    </div>
    <p class="mt-4 text-sm font-medium text-slate-300">No company linked</p>
    <p class="mt-1 text-xs text-slate-500">Your account is not linked to a company yet. Contact an administrator.</p>
  </div>

{:else if company}
  <div class="space-y-6">

    <!-- Header card -->
    <div class="rounded-3xl border border-violet-500/20 bg-slate-900/95 p-6 shadow-xl">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div class="flex items-center gap-3">
            <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-violet-500/20">
              <svg class="h-6 w-6 text-violet-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
                <path stroke-linecap="round" stroke-linejoin="round" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
              </svg>
            </div>
            <div>
              <h2 class="text-xl font-semibold text-white">{company.name}</h2>
              <p class="mt-0.5 font-mono text-xs text-violet-400">FCA {company.fCANumber}</p>
            </div>
          </div>
        </div>
        <div class="flex flex-wrap gap-2">
          <span class="rounded-full bg-slate-800 px-3 py-1 text-xs font-semibold text-slate-300">{company.type}</span>
          {#if company.fcaStatus}
            <span class="rounded-full bg-emerald-500/15 px-3 py-1 text-xs font-semibold text-emerald-400">{company.fcaStatus}</span>
          {/if}
        </div>
      </div>

      <!-- Contact row -->
      <div class="mt-5 flex flex-wrap gap-5 text-sm text-slate-400">
        {#if company.email}
          <a href="mailto:{company.email}" class="flex items-center gap-2 hover:text-white transition">
            <svg class="h-4 w-4 shrink-0 text-slate-600" viewBox="0 0 20 20" fill="currentColor">
              <path d="M3 4a2 2 0 00-2 2v1.161l8.441 4.221a1.25 1.25 0 001.118 0L19 7.162V6a2 2 0 00-2-2H3z"/>
              <path d="M19 8.839l-7.77 3.885a2.75 2.75 0 01-2.46 0L1 8.839V14a2 2 0 002 2h14a2 2 0 002-2V8.839z"/>
            </svg>
            {company.email}
          </a>
        {/if}
        {#if company.phone}
          <a href="tel:{company.phone}" class="flex items-center gap-2 hover:text-white transition">
            <svg class="h-4 w-4 shrink-0 text-slate-600" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M2 3.5A1.5 1.5 0 013.5 2h1.148a1.5 1.5 0 011.465 1.175l.716 3.223a1.5 1.5 0 01-1.052 1.767l-.933.267c-.41.117-.643.555-.48.95a11.542 11.542 0 006.254 6.254c.395.163.833-.07.95-.48l.267-.933a1.5 1.5 0 011.767-1.052l3.223.716A1.5 1.5 0 0118 15.352V16.5a1.5 1.5 0 01-1.5 1.5H15c-1.149 0-2.263-.15-3.326-.43A13.022 13.022 0 012.43 8.326 13.019 13.019 0 012 5V3.5z" clip-rule="evenodd"/>
            </svg>
            {company.phone}
          </a>
        {/if}
        {#if company.website}
          <a href={company.website} target="_blank" rel="noopener noreferrer" class="flex items-center gap-2 hover:text-white transition">
            <svg class="h-4 w-4 shrink-0 text-slate-600" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM4.332 8.027a6.012 6.012 0 011.912-2.706C6.512 5.73 6.974 6 7.5 6A1.5 1.5 0 019 7.5V8a2 2 0 004 0 2 2 0 011.523-1.943A5.977 5.977 0 0116 10c0 .34-.028.675-.083 1H15a2 2 0 00-2 2v2.207l-2.5 2.5A5.987 5.987 0 014.332 8.027z" clip-rule="evenodd"/>
            </svg>
            {company.website}
          </a>
        {/if}
        <span class="flex items-center gap-2">
          <svg class="h-4 w-4 shrink-0 text-slate-600" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M5.75 2a.75.75 0 01.75.75V4h7V2.75a.75.75 0 011.5 0V4h.25A2.75 2.75 0 0118 6.75v8.5A2.75 2.75 0 0115.25 18H4.75A2.75 2.75 0 012 15.25v-8.5A2.75 2.75 0 014.75 4H5V2.75A.75.75 0 015.75 2zm-1 5.5c-.69 0-1.25.56-1.25 1.25v6.5c0 .69.56 1.25 1.25 1.25h10.5c.69 0 1.25-.56 1.25-1.25v-6.5c0-.69-.56-1.25-1.25-1.25H4.75z" clip-rule="evenodd"/>
          </svg>
          Registered {formatDate(company.createdAt)}
        </span>
      </div>
    </div>

    <div class="grid gap-6 xl:grid-cols-2">

      <!-- Addresses -->
      <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
        <h3 class="text-sm font-semibold text-white">Addresses</h3>
        {#if company.addresses?.length}
          <div class="mt-4 space-y-4">
            {#each company.addresses as addr}
              <div class="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                <span class="inline-block rounded-full bg-slate-800 px-2.5 py-0.5 text-xs font-semibold text-slate-400">{addr.type}</span>
                <p class="mt-2 text-sm text-slate-300 leading-relaxed">{formatAddress(addr) || '—'}</p>
              </div>
            {/each}
          </div>
        {:else}
          <p class="mt-3 text-sm text-slate-600">No addresses on record.</p>
        {/if}
      </div>

      <!-- Trading names -->
      <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
        <h3 class="text-sm font-semibold text-white">Trading names</h3>
        {#if company.tradingNames?.length}
          <div class="mt-4 space-y-2">
            {#each company.tradingNames as t}
              <div class="flex items-center justify-between rounded-2xl border border-slate-800 bg-slate-950/60 px-4 py-3">
                <span class="text-sm font-medium text-white">{t.name}</span>
                <div class="flex items-center gap-2 text-xs text-slate-500">
                  {#if t.status}<span>{t.status}</span>{/if}
                  {#if t.effectiveFrom}<span>from {t.effectiveFrom}</span>{/if}
                  {#if t.effectiveTo}<span>to {t.effectiveTo}</span>{/if}
                </div>
              </div>
            {/each}
          </div>
        {:else}
          <p class="mt-3 text-sm text-slate-600">No trading names on record.</p>
        {/if}
      </div>

      <!-- Bank accounts -->
      {#if company.bankAccounts?.length}
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
          <h3 class="text-sm font-semibold text-white">Bank accounts</h3>
          <div class="mt-4 space-y-3">
            {#each company.bankAccounts as b}
              <div class="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                <p class="text-sm font-semibold text-white">{b.accountName}</p>
                <p class="mt-0.5 text-xs text-slate-400">{b.bankName}</p>
                <div class="mt-2 flex gap-4 font-mono text-xs text-slate-400">
                  <span>Account: {b.accountNumber}</span>
                  <span>Sort code: {b.sortCode}</span>
                </div>
              </div>
            {/each}
          </div>
        </div>
      {/if}

      <!-- Parent network -->
      {#if company.parentCompany}
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
          <h3 class="text-sm font-semibold text-white">Network / parent</h3>
          <div class="mt-4 rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
            <p class="font-semibold text-white">{company.parentCompany.name}</p>
            <p class="mt-0.5 font-mono text-xs text-violet-400">FCA {company.parentCompany.fCANumber}</p>
            <div class="mt-2 flex flex-wrap gap-3 text-xs text-slate-400">
              {#if company.parentCompany.email}<span>{company.parentCompany.email}</span>{/if}
              {#if company.parentCompany.phone}<span>{company.parentCompany.phone}</span>{/if}
            </div>
          </div>
        </div>
      {/if}

    </div>
  </div>
{/if}
