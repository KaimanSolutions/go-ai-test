<script>
  import { onMount } from 'svelte';

  let articles = $state([]);
  let selectedArticle = $state(null);
  let searchQuery = $state('');
  let selectedCategory = $state('All');
  let isLoading = $state(true);
  let error = $state('');
  let showEditor = $state(false);
  let isEditingMode = $state(false);
  let isSaving = $state(false);

  let editorState = $state({
    title: '',
    content: '',
    category: 'guides'
  });

  const categories = [
    { label: 'All', value: 'All' },
    { label: 'Getting Started', value: 'getting-started' },
    { label: 'API Docs', value: 'api-docs' },
    { label: 'Troubleshooting', value: 'troubleshooting' },
    { label: 'Guides', value: 'guides' }
  ];

  const categoryOptions = categories.filter(c => c.value !== 'All');

  // Sample articles with categories
  const sampleArticles = [
    {
      id: 1,
      title: 'Getting Started with Form Builder',
      content: '# Getting Started\n\nWelcome to Form Builder! This guide will walk you through creating your first form...\n\n## Step 1: Create a New Form\nClick the "Create New Form" button in the Forms section.\n\n## Step 2: Add Fields\nAdd fields by clicking the "Add Field" button and selecting the field type.\n\n## Step 3: Configure Validation\nSet up validation rules for your fields.\n\n## Step 4: Save and Deploy\nSave your form and deploy it to your application.',
      category: 'getting-started',
      createdAt: new Date('2024-01-15').toISOString()
    },
    {
      id: 2,
      title: 'Understanding Form Schemas',
      content: '# Form Schemas\n\nForm schemas define the structure and behavior of your forms.\n\n## Schema Structure\n- **id**: Unique identifier\n- **title**: Display name\n- **description**: Form description\n- **steps**: Multi-step form sections\n- **fields**: Individual form fields\n\n## Field Types\n- Text input\n- Number input\n- Dropdown\n- Checkbox\n- Radio button\n- Date picker\n\nEach field can have validation rules and dependencies.',
      category: 'guides',
      createdAt: new Date('2024-01-16').toISOString()
    },
    {
      id: 3,
      title: 'API Reference',
      content: '# API Reference\n\n## Endpoints\n\n### Get All Schemas\n`GET /api/formbuilder/schemas`\n\nReturns all available form schemas.\n\n### Save Schema\n`POST /api/formbuilder/schema`\n\nSaves a new form schema. Requires authentication.\n\n### Validate Form\n`POST /api/formbuilder/validate`\n\nValidates form submission data against a schema.\n\n## Authentication\nAll protected endpoints require a valid JWT token in the Authorization header.',
      category: 'api-docs',
      createdAt: new Date('2024-01-17').toISOString()
    },
    {
      id: 4,
      title: 'Troubleshooting Common Issues',
      content: '# Troubleshooting\n\n## Issue: Form validation failing unexpectedly\nCheck that your validation rules are properly configured in the schema.\n\n## Issue: Branding settings not applying\nClear your browser cache and reload the page.\n\n## Issue: Unable to save forms\nEnsure you are authenticated and have the necessary permissions.\n\n## Issue: Performance issues with large forms\nConsider splitting large forms into multiple steps using the form builder steps feature.',
      category: 'troubleshooting',
      createdAt: new Date('2024-01-18').toISOString()
    },
    {
      id: 5,
      title: 'Customizing Branding',
      content: '# Customizing Branding\n\nPersonalize the look and feel of your Form Builder interface.\n\n## Colors\n- **Primary Color**: Main brand color\n- **Accent Color**: Secondary highlights\n- **Background**: Main background\n- **Surface**: Card and panel backgrounds\n- **Text Colors**: Primary and muted text\n\n## Typography\nSelect from multiple font options for headings and body text.\n\n## Applying Changes\nBranding changes take effect immediately across the platform.',
      category: 'guides',
      createdAt: new Date('2024-01-19').toISOString()
    },
    {
      id: 6,
      title: 'Multi-Step Forms',
      content: '# Creating Multi-Step Forms\n\nBreak down complex forms into manageable steps.\n\n## Benefits\n- Improved user experience\n- Better form completion rates\n- Logical flow\n\n## How to Create\n1. Add multiple steps in the form builder\n2. Configure fields for each step\n3. Set up conditional display based on previous answers\n4. Test the complete flow',
      category: 'guides',
      createdAt: new Date('2024-01-20').toISOString()
    }
  ];

  async function loadArticles() {
    try {
      isLoading = true;
      try {
        const response = await fetch('/api/helparticle');
        if (response.ok) {
          const data = await response.json();
          articles = data.length > 0 ? data : sampleArticles;
        } else {
          articles = sampleArticles;
        }
      } catch (apiError) {
        console.warn('Using sample articles:', apiError);
        articles = sampleArticles;
      }
    } catch (err) {
      error = 'Failed to load articles';
      console.error(err);
    } finally {
      isLoading = false;
    }
  }

  function getFilteredArticles() {
    return articles.filter(article => {
      const matchesSearch = article.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
                           article.content.toLowerCase().includes(searchQuery.toLowerCase());
      const matchesCategory = selectedCategory === 'All' || article.category === selectedCategory;
      return matchesSearch && matchesCategory;
    });
  }

  function selectArticle(article) {
    selectedArticle = article;
  }

  function closeArticle() {
    selectedArticle = null;
  }

  function openCreateEditor() {
    isEditingMode = false;
    editorState = {
      title: '',
      content: '',
      category: 'guides'
    };
    showEditor = true;
  }

  function openEditEditor(article) {
    isEditingMode = true;
    editorState = {
      title: article.title,
      content: article.content,
      category: article.category
    };
    showEditor = true;
    selectedArticle = null;
  }

  function closeEditor() {
    showEditor = false;
    editorState = {
      title: '',
      content: '',
      category: 'guides'
    };
  }

  async function saveArticle() {
    if (!editorState.title.trim() || !editorState.content.trim()) {
      error = 'Title and content are required';
      return;
    }

    try {
      isSaving = true;
      error = '';

      const payload = {
        title: editorState.title,
        content: editorState.content,
        category: editorState.category
      };

      const response = await fetch('/api/helparticle', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      });

      if (!response.ok) {
        throw new Error('Failed to save article');
      }

      // Add to local articles list
      const newArticle = await response.json();
      articles = [...articles, newArticle];

      closeEditor();
      // Reload articles to sync with backend
      await loadArticles();
    } catch (err) {
      error = err.message || 'Failed to save article';
      console.error(err);
    } finally {
      isSaving = false;
    }
  }

  async function deleteArticle(articleId) {
    if (!confirm('Are you sure you want to delete this article?')) {
      return;
    }

    try {
      articles = articles.filter(a => a.id !== articleId);
      closeArticle();
      error = '';
    } catch (err) {
      error = 'Failed to delete article';
      console.error(err);
    }
  }

  function renderMarkdown(content) {
    let html = content;

    // Headers
    html = html.replace(/^### (.*?)$/gm, '<h3 class="text-lg font-semibold text-white mt-4 mb-2">$1</h3>');
    html = html.replace(/^## (.*?)$/gm, '<h2 class="text-2xl font-semibold text-white mt-6 mb-3">$1</h2>');
    html = html.replace(/^# (.*?)$/gm, '<h1 class="text-3xl font-semibold text-white mt-8 mb-4">$1</h1>');

    // Bold
    html = html.replace(/\*\*(.*?)\*\*/g, '<strong class="font-semibold text-slate-100">$1</strong>');

    // Code blocks
    html = html.replace(/`([^`]+)`/g, '<code class="bg-slate-900 px-2 py-1 rounded text-sky-300 font-mono text-sm">$1</code>');

    // Lists
    const lines = html.split('\n');
    let inList = false;
    html = lines.map(line => {
      if (line.trim().startsWith('- ')) {
        if (!inList) {
          inList = true;
          return '<ul class="list-disc list-inside space-y-1 ml-2 text-slate-300"><li>' + line.replace(/^- /, '') + '</li>';
        }
        return '<li>' + line.replace(/^- /, '') + '</li>';
      } else {
        if (inList) {
          inList = false;
          return '</ul>' + line;
        }
        return line;
      }
    }).join('\n');

    if (inList) {
      html += '</ul>';
    }

    // Paragraphs
    html = html.replace(/\n\n/g, '</p><p class="text-slate-300 leading-7">');
    if (!html.startsWith('<h') && !html.startsWith('<ul') && !html.startsWith('</ul>')) {
      html = '<p class="text-slate-300 leading-7">' + html + '</p>';
    }

    return html;
  }

  onMount(loadArticles);

  let filteredArticles = $derived(getFilteredArticles());
</script>

<div class="space-y-6">
  {#if error}
    <div class="rounded-3xl border border-red-800 bg-red-950/95 p-6 shadow-xl">
      <p class="text-sm text-red-200">{error}</p>
    </div>
  {/if}

  {#if showEditor}
    <!-- Article Editor Modal -->
    <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <div class="mb-6 flex items-center justify-between">
        <h3 class="text-xl font-semibold text-white">{isEditingMode ? 'Edit Article' : 'Create New Article'}</h3>
        <button
          class="rounded-2xl border border-slate-800 px-3 py-2 text-sm font-semibold text-slate-300 transition hover:bg-slate-800 hover:text-white"
          onclick={closeEditor}
        >
          ✕
        </button>
      </div>

      <div class="grid gap-6 lg:grid-cols-2">
        <!-- Editor Panel -->
        <div class="space-y-4">
          <div>
            <label for="article-title" class="block text-sm font-semibold text-white">Title</label>
            <input
              id="article-title"
              type="text"
              placeholder="Article title..."
              bind:value={editorState.title}
              class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 text-white placeholder-slate-500 shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200"
            />
          </div>

          <div>
            <label for="article-category" class="block text-sm font-semibold text-white">Category</label>
            <select
              id="article-category"
              bind:value={editorState.category}
              class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 text-white shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200"
            >
              {#each categoryOptions as category}
                <option value={category.value}>{category.label}</option>
              {/each}
            </select>
          </div>

          <div class="flex-1">
            <label for="article-content" class="block text-sm font-semibold text-white">Content (Markdown)</label>
            <textarea
              id="article-content"
              placeholder="Write your article in Markdown...&#10;&#10;# Heading 1&#10;## Heading 2&#10;**Bold text**&#10;`Code`&#10;- List item"
              bind:value={editorState.content}
              class="mt-2 h-96 w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 font-mono text-sm text-white placeholder-slate-500 shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200"
            />
          </div>

          <div class="flex gap-3">
            <button
              class="flex-1 rounded-2xl bg-sky-500 px-4 py-3 text-sm font-semibold text-white transition hover:bg-sky-400 disabled:opacity-50 disabled:cursor-not-allowed"
              onclick={saveArticle}
              disabled={isSaving || !editorState.title.trim() || !editorState.content.trim()}
            >
              {isSaving ? 'Saving...' : isEditingMode ? 'Update Article' : 'Create Article'}
            </button>
            <button
              class="rounded-2xl border border-slate-800 px-4 py-3 text-sm font-semibold text-slate-300 transition hover:bg-slate-800 hover:text-white"
              onclick={closeEditor}
            >
              Cancel
            </button>
          </div>
        </div>

        <!-- Preview Panel -->
        <div class="rounded-2xl border border-slate-800 bg-slate-950/90 p-6">
          <h4 class="mb-4 text-sm font-semibold text-slate-400">Preview</h4>
          <div class="prose prose-invert max-w-none max-h-96 overflow-y-auto space-y-3">
            {#if editorState.title}
              <div class="text-2xl font-semibold text-white">{editorState.title}</div>
            {/if}
            <div class="text-xs text-slate-500">{editorState.category.replace('-', ' ')}</div>
            <div class="prose-content">
              {@html renderMarkdown(editorState.content)}
            </div>
          </div>
        </div>
      </div>
    </section>
  {:else if !selectedArticle}
    <!-- Help Centre Browse View -->
    <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <div class="mb-6 flex items-center justify-between">
        <div>
          <h3 class="text-xl font-semibold text-white">Search Help Articles</h3>
          <p class="mt-2 text-sm text-slate-400">Find answers, guides, and documentation.</p>
        </div>
        <button
          class="rounded-2xl bg-sky-500 px-4 py-3 text-sm font-semibold text-white transition hover:bg-sky-400"
          onclick={openCreateEditor}
        >
          + New Article
        </button>
      </div>

      <!-- Search Input -->
      <div class="mb-6">
        <input
          type="text"
          placeholder="Search articles..."
          bind:value={searchQuery}
          class="w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 text-white placeholder-slate-500 shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200"
        />
      </div>

      <!-- Category Filter -->
      <div class="mb-6">
        <div class="flex flex-wrap gap-2">
          {#each categories as category}
            <button
              class="rounded-2xl px-4 py-2 text-sm font-semibold transition {selectedCategory === category.value
                ? 'bg-sky-500 text-white shadow-md'
                : 'border border-slate-800 text-slate-300 hover:bg-slate-800 hover:text-white'}"
              onclick={() => {
                selectedCategory = category.value;
                searchQuery = '';
              }}
            >
              {category.label}
            </button>
          {/each}
        </div>
      </div>

      <!-- Articles Grid -->
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {#if isLoading}
          <div class="col-span-full rounded-2xl border border-slate-800 bg-slate-950/90 p-6 text-center text-slate-400">
            Loading articles...
          </div>
        {:else if filteredArticles.length === 0}
          <div class="col-span-full rounded-2xl border border-slate-800 bg-slate-950/90 p-6 text-center text-slate-400">
            No articles found. Try adjusting your search or filters.
          </div>
        {:else}
          {#each filteredArticles as article (article.id)}
            <button
              class="rounded-3xl border border-slate-800 bg-slate-950/90 p-5 text-left transition hover:border-sky-500 hover:bg-slate-900 hover:shadow-lg"
              onclick={() => selectArticle(article)}
            >
              <div class="flex items-start justify-between gap-3">
                <div class="flex-1">
                  <h4 class="font-semibold text-white hover:text-sky-400">{article.title}</h4>
                  <p class="mt-2 line-clamp-2 text-sm text-slate-400">
                    {article.content.split('\n')[1] || article.content.substring(0, 100)}
                  </p>
                </div>
                <span class="shrink-0 rounded-lg bg-slate-800 px-2 py-1 text-xs font-medium text-sky-400">
                  {article.category.replace('-', ' ')}
                </span>
              </div>
              <div class="mt-4 text-xs text-slate-500">
                {new Date(article.createdAt).toLocaleDateString()}
              </div>
            </button>
          {/each}
        {/if}
      </div>
    </section>
  {:else}
    <!-- Article Detail View -->
    <section class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <div class="mb-4 flex items-center justify-between gap-3">
        <button
          class="rounded-2xl border border-slate-800 px-4 py-2 text-sm font-semibold text-slate-300 transition hover:bg-slate-800 hover:text-white"
          onclick={closeArticle}
        >
          ← Back to Help Centre
        </button>
        <div class="flex gap-2">
          <button
            class="rounded-2xl bg-sky-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-sky-400"
            onclick={() => openEditEditor(selectedArticle)}
          >
            Edit
          </button>
          <button
            class="rounded-2xl border border-red-800 px-4 py-2 text-sm font-semibold text-red-400 transition hover:bg-red-950"
            onclick={() => deleteArticle(selectedArticle.id)}
          >
            Delete
          </button>
        </div>
      </div>

      <div class="rounded-2xl border border-slate-800 bg-slate-950/90 p-6 sm:p-8">
        <div class="mb-6">
          <h2 class="text-3xl font-semibold text-white">{selectedArticle.title}</h2>
          <div class="mt-3 flex items-center gap-3 text-sm text-slate-400">
            <span>{new Date(selectedArticle.createdAt).toLocaleDateString()}</span>
            <span class="inline-block rounded-lg bg-slate-800 px-2 py-1 text-xs font-medium text-sky-400">
              {selectedArticle.category.replace('-', ' ')}
            </span>
          </div>
        </div>

        <div class="prose prose-invert max-w-none space-y-4">
          {@html renderMarkdown(selectedArticle.content)}
        </div>
      </div>

      <!-- Related Articles -->
      {#if articles.length > 1}
        <div class="mt-8">
          <h3 class="mb-4 text-lg font-semibold text-white">Related Articles</h3>
          <div class="grid gap-4 sm:grid-cols-2">
            {#each articles
              .filter(a => a.category === selectedArticle.category && a.id !== selectedArticle.id)
              .slice(0, 2) as relatedArticle}
              <button
                class="rounded-2xl border border-slate-800 bg-slate-950/90 p-4 text-left transition hover:border-sky-500 hover:bg-slate-900"
                onclick={() => selectArticle(relatedArticle)}
              >
                <h4 class="font-semibold text-white hover:text-sky-400">{relatedArticle.title}</h4>
                <p class="mt-1 text-xs text-slate-500">{relatedArticle.category.replace('-', ' ')}</p>
              </button>
            {/each}
          </div>
        </div>
      {/if}
    </section>
  {/if}
</div>

<style>
  :global(.prose-content h1) {
    @apply text-3xl font-semibold text-white mt-6 mb-3;
  }

  :global(.prose-content h2) {
    @apply text-2xl font-semibold text-white mt-4 mb-2;
  }

  :global(.prose-content h3) {
    @apply text-lg font-semibold text-white mt-3 mb-1;
  }

  :global(.prose-content code) {
    @apply bg-slate-900 px-2 py-1 rounded text-sky-300 font-mono text-sm;
  }

  :global(.prose-content p) {
    @apply text-slate-300 leading-7;
  }
</style>