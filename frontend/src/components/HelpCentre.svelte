<script>
  import { onMount } from 'svelte';

  let { token = '', role = '' } = $props();

  const isAdmin = $derived(role === 'Admin');

  let articles        = $state([]);
  let selectedArticle = $state(null);
  let searchQuery     = $state('');
  let selectedCategory = $state('All');
  let isLoading       = $state(true);
  let error           = $state('');
  let showEditor      = $state(false);
  let isEditingMode   = $state(false);
  let isSaving        = $state(false);

  let editorState = $state({
    title: '',
    content: '',
    category: 'guides',
    showInAdminPortal:    true,
    showInBrokerPortal:   true,
    showInCustomerPortal: true
  });

  const categories = [
    { label: 'All',              value: 'All' },
    { label: 'Getting Started',  value: 'getting-started' },
    { label: 'API Docs',         value: 'api-docs' },
    { label: 'Troubleshooting',  value: 'troubleshooting' },
    { label: 'Guides',           value: 'guides' }
  ];

  const categoryOptions = categories.filter(c => c.value !== 'All');

  async function loadArticles() {
    try {
      isLoading = true;
      error = '';
      const headers = token ? { Authorization: `Bearer ${token}` } : {};
      const response = await fetch('/api/helparticle', { headers });
      if (!response.ok) throw new Error(await response.text());
      articles = await response.json();
    } catch (err) {
      error = 'Failed to load articles.';
      console.error(err);
    } finally {
      isLoading = false;
    }
  }

  let filteredArticles = $derived(
    articles.filter(a => {
      const matchesSearch = !searchQuery ||
        a.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        a.content.toLowerCase().includes(searchQuery.toLowerCase());
      const matchesCategory = selectedCategory === 'All' || a.category === selectedCategory;
      return matchesSearch && matchesCategory;
    })
  );

  function selectArticle(article) { selectedArticle = article; }
  function closeArticle()         { selectedArticle = null; }

  function openCreateEditor() {
    isEditingMode = false;
    editorState = { title: '', content: '', category: 'guides', showInAdminPortal: true, showInBrokerPortal: true, showInCustomerPortal: true };
    showEditor = true;
  }

  function openEditEditor(article) {
    isEditingMode = true;
    editorState = {
      title:                article.title,
      content:              article.content,
      category:             article.category,
      showInAdminPortal:    article.showInAdminPortal    ?? true,
      showInBrokerPortal:   article.showInBrokerPortal   ?? true,
      showInCustomerPortal: article.showInCustomerPortal ?? true
    };
    showEditor = true;
    selectedArticle = null;
  }

  function closeEditor() {
    showEditor = false;
  }

  async function saveArticle() {
    if (!editorState.title.trim() || !editorState.content.trim()) {
      error = 'Title and content are required.';
      return;
    }

    try {
      isSaving = true;
      error = '';

      const method = isEditingMode && selectedArticle ? 'PUT' : 'POST';
      const url    = isEditingMode && selectedArticle
        ? `/api/helparticle/${selectedArticle.id}`
        : '/api/helparticle';

      const response = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify(editorState)
      });

      if (!response.ok) throw new Error(await response.text());

      closeEditor();
      await loadArticles();
    } catch (err) {
      error = err.message || 'Failed to save article.';
    } finally {
      isSaving = false;
    }
  }

  async function deleteArticle(articleId) {
    if (!confirm('Are you sure you want to delete this article?')) return;

    try {
      error = '';
      const response = await fetch(`/api/helparticle/${articleId}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${token}` }
      });
      if (!response.ok) throw new Error(await response.text());
      closeArticle();
      await loadArticles();
    } catch (err) {
      error = err.message || 'Failed to delete article.';
    }
  }

  function renderMarkdown(content) {
    let html = content;
    html = html.replace(/^### (.*?)$/gm, '<h3 class="text-lg font-semibold text-white mt-4 mb-2">$1</h3>');
    html = html.replace(/^## (.*?)$/gm,  '<h2 class="text-2xl font-semibold text-white mt-6 mb-3">$1</h2>');
    html = html.replace(/^# (.*?)$/gm,   '<h1 class="text-3xl font-semibold text-white mt-8 mb-4">$1</h1>');
    html = html.replace(/\*\*(.*?)\*\*/g, '<strong class="font-semibold text-slate-100">$1</strong>');
    html = html.replace(/`([^`]+)`/g, '<code class="bg-slate-900 px-2 py-1 rounded text-sky-300 font-mono text-sm">$1</code>');

    const lines = html.split('\n');
    let inList = false;
    html = lines.map(line => {
      if (line.trim().startsWith('- ')) {
        if (!inList) { inList = true; return '<ul class="list-disc list-inside space-y-1 ml-2 text-slate-300"><li>' + line.replace(/^- /, '') + '</li>'; }
        return '<li>' + line.replace(/^- /, '') + '</li>';
      } else {
        if (inList) { inList = false; return '</ul>' + line; }
        return line;
      }
    }).join('\n');
    if (inList) html += '</ul>';

    html = html.replace(/\n\n/g, '</p><p class="text-slate-300 leading-7">');
    if (!html.startsWith('<h') && !html.startsWith('<ul') && !html.startsWith('</ul>'))
      html = '<p class="text-slate-300 leading-7">' + html + '</p>';

    return html;
  }

  onMount(loadArticles);
</script>

<div class="space-y-6">
  {#if error}
    <div class="rounded-3xl border border-red-800 bg-red-950/95 p-4 text-sm text-red-300">{error}</div>
  {/if}

  {#if showEditor && isAdmin}
    <!-- Article editor (admin only) -->
    <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <div class="mb-6 flex items-center justify-between">
        <h3 class="text-xl font-semibold text-white">{isEditingMode ? 'Edit Article' : 'New Article'}</h3>
        <button class="rounded-2xl border border-slate-800 px-3 py-2 text-sm font-semibold text-slate-300 transition hover:bg-slate-800" onclick={closeEditor}>✕</button>
      </div>

      <div class="grid gap-6 lg:grid-cols-2">
        <!-- Editor panel -->
        <div class="space-y-4">
          <div>
            <label for="article-title" class="block text-sm font-semibold text-white">Title</label>
            <input id="article-title" type="text" placeholder="Article title..." bind:value={editorState.title}
              class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 text-white placeholder-slate-500 focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200" />
          </div>

          <div>
            <label for="article-category" class="block text-sm font-semibold text-white">Category</label>
            <select id="article-category" bind:value={editorState.category}
              class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 text-white focus:border-sky-500 focus:outline-none">
              {#each categoryOptions as cat}
                <option value={cat.value}>{cat.label}</option>
              {/each}
            </select>
          </div>

          <!-- Visibility -->
          <div>
            <p class="text-sm font-semibold text-white">Visibility</p>
            <div class="mt-3 space-y-2">
              {#each [
                { key: 'showInAdminPortal',    label: 'Admin portal',    color: 'text-slate-400' },
                { key: 'showInBrokerPortal',   label: 'Broker portal',   color: 'text-violet-400' },
                { key: 'showInCustomerPortal', label: 'Customer portal', color: 'text-sky-400' }
              ] as vis}
                <label class="flex cursor-pointer items-center gap-3">
                  <div class="relative">
                    <input type="checkbox" bind:checked={editorState[vis.key]} class="sr-only" />
                    <div class="h-5 w-9 rounded-full transition {editorState[vis.key] ? 'bg-sky-500' : 'bg-slate-700'}"></div>
                    <div class="absolute top-0.5 left-0.5 h-4 w-4 rounded-full bg-white shadow transition-transform {editorState[vis.key] ? 'translate-x-4' : 'translate-x-0'}"></div>
                  </div>
                  <span class="text-sm {vis.color}">{vis.label}</span>
                </label>
              {/each}
            </div>
          </div>

          <div>
            <label for="article-content" class="block text-sm font-semibold text-white">Content (Markdown)</label>
            <textarea id="article-content" placeholder="Write your article in Markdown..." bind:value={editorState.content}
              class="mt-2 h-72 w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 font-mono text-sm text-white placeholder-slate-500 focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200"></textarea>
          </div>

          <div class="flex gap-3">
            <button onclick={saveArticle} disabled={isSaving || !editorState.title.trim() || !editorState.content.trim()}
              class="flex-1 rounded-2xl bg-sky-500 px-4 py-3 text-sm font-semibold text-white transition hover:bg-sky-400 disabled:cursor-not-allowed disabled:opacity-50">
              {isSaving ? 'Saving…' : isEditingMode ? 'Update Article' : 'Create Article'}
            </button>
            <button onclick={closeEditor} class="rounded-2xl border border-slate-800 px-4 py-3 text-sm font-semibold text-slate-300 transition hover:bg-slate-800">Cancel</button>
          </div>
        </div>

        <!-- Preview panel -->
        <div class="rounded-2xl border border-slate-800 bg-slate-950/90 p-6">
          <h4 class="mb-4 text-sm font-semibold text-slate-400">Preview</h4>
          <div class="max-h-[480px] space-y-3 overflow-y-auto">
            {#if editorState.title}
              <p class="text-2xl font-semibold text-white">{editorState.title}</p>
            {/if}
            <p class="text-xs text-slate-500">{editorState.category.replace('-', ' ')}</p>
            <div class="prose-content">
              {@html renderMarkdown(editorState.content)}
            </div>
          </div>
        </div>
      </div>
    </section>

  {:else if !selectedArticle}
    <!-- Browse view -->
    <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <div class="mb-6 flex items-center justify-between">
        <div>
          <h3 class="text-xl font-semibold text-white">Help Centre</h3>
          <p class="mt-1 text-sm text-slate-400">Find answers, guides, and documentation.</p>
        </div>
        {#if isAdmin}
          <button onclick={openCreateEditor} class="rounded-2xl bg-sky-500 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-sky-400">+ New Article</button>
        {/if}
      </div>

      <input type="text" placeholder="Search articles…" bind:value={searchQuery}
        class="mb-5 w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 text-white placeholder-slate-500 focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200" />

      <div class="mb-5 flex flex-wrap gap-2">
        {#each categories as cat}
          <button
            onclick={() => { selectedCategory = cat.value; searchQuery = ''; }}
            class="rounded-2xl px-4 py-2 text-sm font-semibold transition {selectedCategory === cat.value ? 'bg-sky-500 text-white' : 'border border-slate-800 text-slate-300 hover:bg-slate-800 hover:text-white'}">
            {cat.label}
          </button>
        {/each}
      </div>

      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {#if isLoading}
          <p class="col-span-full py-8 text-center text-sm text-slate-500">Loading articles…</p>
        {:else if filteredArticles.length === 0}
          <p class="col-span-full py-8 text-center text-sm text-slate-500">No articles found.</p>
        {:else}
          {#each filteredArticles as article (article.id)}
            <button onclick={() => selectArticle(article)}
              class="rounded-3xl border border-slate-800 bg-slate-950/90 p-5 text-left transition hover:border-sky-500 hover:shadow-lg">
              <div class="flex items-start justify-between gap-3">
                <div class="flex-1">
                  <h4 class="font-semibold text-white">{article.title}</h4>
                  <p class="mt-2 line-clamp-2 text-sm text-slate-400">{article.content.split('\n')[1] || article.content.substring(0, 100)}</p>
                </div>
                <span class="shrink-0 rounded-lg bg-slate-800 px-2 py-1 text-xs font-medium text-sky-400">{article.category.replace('-', ' ')}</span>
              </div>
              {#if isAdmin}
                <div class="mt-3 flex gap-1.5">
                  {#if article.showInAdminPortal}    <span class="rounded-md bg-slate-800 px-2 py-0.5 text-xs text-slate-400">Admin</span>{/if}
                  {#if article.showInBrokerPortal}   <span class="rounded-md bg-violet-900/50 px-2 py-0.5 text-xs text-violet-400">Broker</span>{/if}
                  {#if article.showInCustomerPortal} <span class="rounded-md bg-sky-900/50 px-2 py-0.5 text-xs text-sky-400">Customer</span>{/if}
                </div>
              {/if}
              <p class="mt-3 text-xs text-slate-600">{new Date(article.createdAt).toLocaleDateString()}</p>
            </button>
          {/each}
        {/if}
      </div>
    </section>

  {:else}
    <!-- Article detail view -->
    <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <div class="mb-4 flex items-center justify-between gap-3">
        <button onclick={closeArticle}
          class="rounded-2xl border border-slate-800 px-4 py-2 text-sm font-semibold text-slate-300 transition hover:bg-slate-800">
          ← Back
        </button>
        {#if isAdmin}
          <div class="flex gap-2">
            <button onclick={() => openEditEditor(selectedArticle)} class="rounded-2xl bg-sky-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-sky-400">Edit</button>
            <button onclick={() => deleteArticle(selectedArticle.id)} class="rounded-2xl border border-red-800 px-4 py-2 text-sm font-semibold text-red-400 transition hover:bg-red-950">Delete</button>
          </div>
        {/if}
      </div>

      <div class="rounded-2xl border border-slate-800 bg-slate-950/90 p-6 sm:p-8">
        <h2 class="text-3xl font-semibold text-white">{selectedArticle.title}</h2>
        <div class="mt-3 flex items-center gap-3 text-sm text-slate-400">
          <span>{new Date(selectedArticle.createdAt).toLocaleDateString()}</span>
          <span class="rounded-lg bg-slate-800 px-2 py-1 text-xs font-medium text-sky-400">{selectedArticle.category.replace('-', ' ')}</span>
          {#if isAdmin}
            {#if selectedArticle.showInAdminPortal}    <span class="rounded-md bg-slate-800 px-2 py-0.5 text-xs text-slate-400">Admin</span>{/if}
            {#if selectedArticle.showInBrokerPortal}   <span class="rounded-md bg-violet-900/50 px-2 py-0.5 text-xs text-violet-400">Broker</span>{/if}
            {#if selectedArticle.showInCustomerPortal} <span class="rounded-md bg-sky-900/50 px-2 py-0.5 text-xs text-sky-400">Customer</span>{/if}
          {/if}
        </div>
        <div class="prose prose-invert mt-6 max-w-none space-y-4">
          {@html renderMarkdown(selectedArticle.content)}
        </div>
      </div>

      {#if articles.filter(a => a.category === selectedArticle.category && a.id !== selectedArticle.id).length > 0}
        <div class="mt-8">
          <h3 class="mb-4 text-lg font-semibold text-white">Related articles</h3>
          <div class="grid gap-4 sm:grid-cols-2">
            {#each articles.filter(a => a.category === selectedArticle.category && a.id !== selectedArticle.id).slice(0, 2) as related}
              <button onclick={() => selectArticle(related)}
                class="rounded-2xl border border-slate-800 bg-slate-950/90 p-4 text-left transition hover:border-sky-500">
                <h4 class="font-semibold text-white">{related.title}</h4>
                <p class="mt-1 text-xs text-slate-500">{related.category.replace('-', ' ')}</p>
              </button>
            {/each}
          </div>
        </div>
      {/if}
    </section>
  {/if}
</div>

<style>
  :global(.prose-content h1) { @apply text-3xl font-semibold text-white mt-6 mb-3; }
  :global(.prose-content h2) { @apply text-2xl font-semibold text-white mt-4 mb-2; }
  :global(.prose-content h3) { @apply text-lg font-semibold text-white mt-3 mb-1; }
  :global(.prose-content code) { @apply bg-slate-900 px-2 py-1 rounded text-sky-300 font-mono text-sm; }
  :global(.prose-content p) { @apply text-slate-300 leading-7; }
</style>
