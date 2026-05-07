<script>
  import { onMount } from 'svelte';
  import { fetchTemplates, fetchTemplate, saveTemplate, deleteTemplate, compileMjml } from '../lib/auth.js';
  import { MJML_STARTER, SMS_STARTER, DOC_STARTER, TEMPLATE_VARIABLES } from '../lib/templateStarters.js';

  let { token = '' } = $props();

  let activeTab  = $state('Email');   // Email | SMS | Document
  let view       = $state('list');    // list | edit
  let templates  = $state([]);
  let loading    = $state(true);
  let saving     = $state(false);
  let previewing = $state(false);
  let error      = $state('');
  let editingId  = $state(null);

  // ── Editor fields ──────────────────────────────────────────────────────────
  let name         = $state('');
  let description  = $state('');
  let subject      = $state('');
  let content      = $state('');
  let isActive     = $state(true);

  // ── Preview ────────────────────────────────────────────────────────────────
  let previewHtml  = $state('');
  let previewError = $state('');
  let showPreview  = $state(false);

  const TABS = ['Email', 'SMS', 'Document'];


  // ── Derived ────────────────────────────────────────────────────────────────
  const filteredTemplates = $derived(templates.filter(t => t.templateType === activeTab));
  const smsChars          = $derived(content.length);
  const smsMessages       = $derived(Math.max(1, Math.ceil(smsChars / 160)));

  onMount(() => loadTemplates());

  async function loadTemplates() {
    loading = true;
    const res = await fetchTemplates(token);
    loading = false;
    if (Array.isArray(res)) templates = res;
  }

  function openNew() {
    editingId   = null;
    name        = '';
    description = '';
    subject     = '';
    content     = activeTab === 'Email' ? MJML_STARTER : activeTab === 'SMS' ? SMS_STARTER : DOC_STARTER;
    isActive    = true;
    previewHtml = '';
    previewError = '';
    showPreview  = false;
    error        = '';
    view         = 'edit';
  }

  async function openEdit(id) {
    error = '';
    const res = await fetchTemplate(id, token);
    if (res.error) { error = res.error; return; }
    editingId    = id;
    name         = res.name;
    description  = res.description;
    subject      = res.subject;
    content      = res.content;
    isActive     = res.isActive;
    previewHtml  = '';
    previewError = '';
    showPreview  = false;
    view         = 'edit';
  }

  async function save() {
    if (!name.trim()) { error = 'Name is required.'; return; }
    saving = true; error = '';
    const res = await saveTemplate({
      id: editingId ?? undefined,
      name, description, templateType: activeTab, subject, content, isActive
    }, token);
    saving = false;
    if (res.error) { error = res.error; return; }
    await loadTemplates();
    view = 'list';
  }

  async function confirmDelete(id, tName) {
    if (!confirm(`Delete template "${tName}"? This cannot be undone.`)) return;
    const res = await deleteTemplate(id, token);
    if (!res.error) templates = templates.filter(t => t.id !== id);
  }

  async function preview() {
    previewError = '';
    previewing   = true;
    if (activeTab === 'Email') {
      const res = await compileMjml(content, token);
      previewing = false;
      if (res.error) { previewError = res.error; showPreview = false; return; }
      previewHtml = res.html;
    } else {
      previewing  = false;
      previewHtml = content;
    }
    showPreview = true;
  }

  function insertVariable(v) {
    content += content.endsWith(' ') || content === '' ? v : ` ${v}`;
  }

  const ic = 'w-full rounded-2xl border border-slate-700 bg-slate-900 px-3 py-2.5 text-sm text-white placeholder-slate-500 focus:border-sky-500 focus:outline-none';
</script>

<!-- ── List view ─────────────────────────────────────────────────────────────── -->
{#if view === 'list'}
  <div class="space-y-6">

    <!-- Header -->
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h2 class="text-lg font-semibold text-white">Templates</h2>
        <p class="mt-0.5 text-sm text-slate-400">Manage email, SMS and document templates.</p>
      </div>
      <button
        onclick={openNew}
        class="flex items-center gap-2 rounded-2xl bg-sky-500 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-sky-400"
      >
        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
          <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z"/>
        </svg>
        New template
      </button>
    </div>

    {#if error}
      <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</p>
    {/if}

    <!-- Tabs -->
    <div class="flex gap-1 rounded-2xl border border-slate-800 bg-slate-900/60 p-1">
      {#each TABS as tab}
        <button
          onclick={() => { activeTab = tab; }}
          class="flex-1 rounded-xl py-2 text-sm font-semibold transition
            {activeTab === tab ? 'bg-slate-700 text-white' : 'text-slate-400 hover:text-white'}"
        >{tab}</button>
      {/each}
    </div>

    {#if loading}
      <div class="flex justify-center py-12">
        <div class="h-6 w-6 animate-spin rounded-full border-2 border-sky-500 border-t-transparent"></div>
      </div>
    {:else if filteredTemplates.length === 0}
      <div class="flex flex-col items-center justify-center rounded-3xl border border-slate-800 bg-slate-900/95 py-16 text-center">
        <div class="flex h-14 w-14 items-center justify-center rounded-full bg-slate-800">
          <svg class="h-6 w-6 text-slate-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"/>
          </svg>
        </div>
        <p class="mt-4 text-sm font-medium text-slate-300">No {activeTab.toLowerCase()} templates yet</p>
        <button onclick={openNew} class="mt-4 rounded-2xl bg-sky-500 px-5 py-2.5 text-sm font-semibold text-white hover:bg-sky-400 transition">
          Create first template
        </button>
      </div>
    {:else}
      <div class="overflow-hidden rounded-3xl border border-slate-800 bg-slate-900/95">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-800">
              <th class="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Name</th>
              {#if activeTab === 'Email'}
                <th class="hidden px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500 md:table-cell">Subject</th>
              {/if}
              <th class="hidden px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500 sm:table-cell">Status</th>
              <th class="hidden px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500 lg:table-cell">Updated</th>
              <th class="px-5 py-3.5"></th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-800">
            {#each filteredTemplates as t}
              <tr class="transition hover:bg-slate-800/40">
                <td class="px-5 py-4">
                  <p class="font-medium text-white">{t.name}</p>
                  {#if t.description}
                    <p class="mt-0.5 text-xs text-slate-500 line-clamp-1">{t.description}</p>
                  {/if}
                </td>
                {#if activeTab === 'Email'}
                  <td class="hidden px-5 py-4 text-slate-400 md:table-cell">{t.subject || '—'}</td>
                {/if}
                <td class="hidden px-5 py-4 sm:table-cell">
                  {#if t.isActive}
                    <span class="rounded-full bg-emerald-500/15 px-2.5 py-1 text-xs font-semibold text-emerald-400">Active</span>
                  {:else}
                    <span class="rounded-full bg-slate-700 px-2.5 py-1 text-xs font-semibold text-slate-400">Inactive</span>
                  {/if}
                </td>
                <td class="hidden px-5 py-4 text-xs text-slate-500 lg:table-cell">
                  {new Date(t.updatedAt).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })}
                </td>
                <td class="px-5 py-4 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <button
                      onclick={() => openEdit(t.id)}
                      class="rounded-xl border border-slate-700 px-3 py-1.5 text-xs font-semibold text-slate-300 transition hover:border-sky-500/50 hover:text-sky-400"
                    >Edit</button>
                    <button
                      onclick={() => confirmDelete(t.id, t.name)}
                      class="rounded-xl border border-red-900/40 px-3 py-1.5 text-xs font-semibold text-red-500 transition hover:bg-red-950"
                    >Delete</button>
                  </div>
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
  </div>

<!-- ── Editor view ────────────────────────────────────────────────────────────── -->
{:else}
  <div class="space-y-6">

    <!-- Editor header -->
    <div class="flex items-center gap-4">
      <button
        onclick={() => { view = 'list'; error = ''; }}
        class="flex items-center gap-1.5 text-xs font-medium text-slate-400 transition hover:text-white"
      >
        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd"/>
        </svg>
        Back to templates
      </button>
      <span class="text-slate-600">·</span>
      <span class="rounded-full px-2.5 py-1 text-xs font-semibold
        {activeTab === 'Email' ? 'bg-sky-500/20 text-sky-400' : activeTab === 'SMS' ? 'bg-violet-500/20 text-violet-400' : 'bg-amber-500/20 text-amber-400'}">
        {activeTab} template
      </span>
    </div>

    {#if error}
      <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</p>
    {/if}

    <div class="grid gap-6 xl:grid-cols-[1fr_260px]">

      <!-- Left: main editor -->
      <div class="space-y-5">

        <!-- Meta fields -->
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
          <h3 class="mb-4 text-sm font-semibold text-white">Details</h3>
          <div class="grid gap-4 sm:grid-cols-2">
            <div class="sm:col-span-2">
              <label class="mb-1.5 block text-xs font-medium text-slate-400">Name *</label>
              <input class={ic} bind:value={name} placeholder="e.g. Application Received" />
            </div>
            <div class="sm:col-span-2">
              <label class="mb-1.5 block text-xs font-medium text-slate-400">Description</label>
              <input class={ic} bind:value={description} placeholder="Internal notes about this template" />
            </div>
            {#if activeTab === 'Email'}
              <div class="sm:col-span-2">
                <label class="mb-1.5 block text-xs font-medium text-slate-400">Subject line</label>
                <input class={ic} bind:value={subject} placeholder="e.g. Your application {{reference}} has been received" />
              </div>
            {/if}
            <div class="flex items-center gap-3">
              <input id="tmpl-active" type="checkbox" class="rounded border-slate-700 bg-slate-900 text-sky-500" bind:checked={isActive} />
              <label for="tmpl-active" class="text-sm text-slate-300 select-none">Active</label>
            </div>
          </div>
        </div>

        <!-- Content editor -->
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
          <div class="mb-4 flex items-center justify-between gap-3">
            <div>
              <h3 class="text-sm font-semibold text-white">
                {activeTab === 'Email' ? 'MJML content' : activeTab === 'SMS' ? 'Message content' : 'Document content'}
              </h3>
              {#if activeTab === 'Email'}
                <p class="mt-0.5 text-xs text-slate-500">Write responsive email using <a href="https://mjml.io/documentation" target="_blank" class="text-sky-400 hover:underline">MJML</a></p>
              {/if}
            </div>
            <div class="flex items-center gap-2">
              {#if activeTab === 'SMS'}
                <span class="text-xs text-slate-500">{smsChars} chars · {smsMessages} msg{smsMessages > 1 ? 's' : ''}</span>
              {/if}
              <button
                onclick={preview}
                disabled={previewing || !content.trim()}
                class="flex items-center gap-1.5 rounded-xl border border-slate-700 px-3 py-1.5 text-xs font-semibold text-slate-300 transition hover:border-sky-500/50 hover:text-sky-400 disabled:opacity-40"
              >
                {#if previewing}
                  <svg class="h-3.5 w-3.5 animate-spin" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M21 12a9 9 0 11-6.219-8.56"/>
                  </svg>
                {:else}
                  <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                    <path d="M10 12.5a2.5 2.5 0 100-5 2.5 2.5 0 000 5z"/>
                    <path fill-rule="evenodd" d="M.664 10.59a1.651 1.651 0 010-1.186A10.004 10.004 0 0110 3c4.257 0 7.893 2.66 9.336 6.41.147.381.146.804 0 1.186A10.004 10.004 0 0110 17c-4.257 0-7.893-2.66-9.336-6.41zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd"/>
                  </svg>
                {/if}
                Preview
              </button>
            </div>
          </div>

          <textarea
            class="w-full rounded-2xl border border-slate-700 bg-slate-950 px-4 py-3 font-mono text-xs text-slate-200 placeholder-slate-600 focus:border-sky-500 focus:outline-none"
            style="min-height: 420px; resize: vertical; tab-size: 2;"
            bind:value={content}
            placeholder={activeTab === 'Email' ? '<mjml>…</mjml>' : activeTab === 'SMS' ? 'Your SMS message here…' : '<h1>Document title</h1>…'}
            spellcheck="false"
          ></textarea>

          {#if previewError}
            <p class="mt-2 text-xs text-red-400">{previewError}</p>
          {/if}
        </div>

        <!-- Preview pane -->
        {#if showPreview && previewHtml}
          <div class="rounded-3xl border border-slate-800 bg-slate-900/95 shadow-xl overflow-hidden">
            <div class="flex items-center justify-between border-b border-slate-800 px-5 py-3">
              <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">Preview</p>
              <button onclick={() => showPreview = false} class="text-xs text-slate-500 hover:text-white">Close</button>
            </div>
            {#if activeTab === 'SMS'}
              <div class="p-6">
                <div class="inline-block max-w-sm rounded-2xl bg-slate-800 px-4 py-3 text-sm text-slate-200 whitespace-pre-wrap">{previewHtml}</div>
              </div>
            {:else}
              <iframe
                srcdoc={previewHtml}
                sandbox="allow-same-origin"
                class="h-[600px] w-full border-0 bg-white"
                title="Template preview"
              ></iframe>
            {/if}
          </div>
        {/if}

        <!-- Save / cancel -->
        <div class="flex items-center justify-end gap-3">
          <button
            onclick={() => { view = 'list'; error = ''; }}
            class="rounded-2xl border border-slate-700 px-5 py-2.5 text-sm font-semibold text-slate-300 transition hover:bg-slate-800"
          >Cancel</button>
          <button
            onclick={save}
            disabled={saving}
            class="rounded-2xl bg-sky-500 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-sky-400 disabled:opacity-50"
          >{saving ? 'Saving…' : editingId ? 'Save changes' : 'Create template'}</button>
        </div>
      </div>

      <!-- Right: variables sidebar -->
      <div class="space-y-4">
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
          <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">Available variables</p>
          <p class="mt-1.5 text-xs text-slate-600">Click to append to content</p>
          <div class="mt-3 space-y-1.5">
            {#each TEMPLATE_VARIABLES as v}
              <button
                onclick={() => insertVariable(v.key)}
                class="group flex w-full items-center justify-between rounded-xl border border-slate-800 px-3 py-2 text-left transition hover:border-sky-500/40 hover:bg-sky-500/5"
              >
                <span class="font-mono text-xs text-sky-400">{v.key}</span>
                <span class="text-xs text-slate-600 group-hover:text-slate-400">{v.label}</span>
              </button>
            {/each}
          </div>
        </div>

        {#if activeTab === 'Email'}
          <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
            <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">MJML tips</p>
            <ul class="mt-3 space-y-2 text-xs text-slate-500">
              <li>• Use <code class="text-sky-400">mj-section</code> for rows</li>
              <li>• Use <code class="text-sky-400">mj-column</code> for columns inside rows</li>
              <li>• Use <code class="text-sky-400">mj-text</code> for text blocks</li>
              <li>• Use <code class="text-sky-400">mj-button</code> for call-to-action buttons</li>
              <li>• Use <code class="text-sky-400">mj-image</code> for images (requires a public URL)</li>
              <li>• Use <code class="text-sky-400">mj-divider</code> for horizontal rules</li>
            </ul>
          </div>
        {/if}

        {#if activeTab === 'SMS'}
          <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
            <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">SMS guidelines</p>
            <ul class="mt-3 space-y-2 text-xs text-slate-500">
              <li>• Standard SMS: 160 characters</li>
              <li>• Unicode (emoji/special chars): 70 characters</li>
              <li>• Longer messages are split automatically</li>
              <li>• Variables are replaced before sending</li>
            </ul>
          </div>
        {/if}
      </div>

    </div>
  </div>
{/if}
