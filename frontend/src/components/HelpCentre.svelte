<script>
  import { onMount } from 'svelte';
  import { marked } from 'marked';


  let article = $state({
    title: '',
    content: ''
  });
  let preview = $state('');
  let error = $state('');

  function updatePreview() {
    preview = marked(article.content);
  }

  async function saveArticle() {
    try {
      const response = await fetch('/api/help-articles', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(article)
      });

      if (!response.ok) {
        throw new Error('Failed to save article');
      }

      alert('Article saved successfully!');
      article = { title: '', content: '' };
      preview = '';
    } catch (err) {
      error = err.message;
    }
  }
</script>

<main class="p-4">
  <h1 class="text-xl font-bold">Help Centre</h1>
  {#if error}
    <p class="text-red-500">{error}</p>
  {/if}
  <div class="mt-4">
    <label for="title">Title</label>
    <input id="title" type="text" bind:value={article.title} class="w-full p-2 border rounded" />
  </div>
  <div class="mt-4">
    <label for="content">Content (Markdown)</label>
    <textarea id="content" bind:value={article.content} class="w-full p-2 border rounded h-64" oninput={updatePreview}></textarea>
  </div>
  <button class="mt-4 p-2 bg-blue-500 text-white rounded" onclick={saveArticle}>Save Article</button>

  <h2 class="mt-8 text-lg font-bold">Preview</h2>
  <div class="mt-4 p-4 border rounded" contenteditable="true" bind:innerHTML={preview}></div>
</main>

<style>
  label {
    display: block;
    margin-bottom: 0.5rem;
    font-weight: bold;
  }
  input, textarea {
    width: 100%;
    padding: 0.5rem;
    border: 1px solid #ccc;
    border-radius: 0.25rem;
  }
</style>