// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import starlightUtils from '@lorenzo_lewis/starlight-utils';

import tailwindcss from '@tailwindcss/vite';

// https://astro.build/config
export default defineConfig({
  site: 'https://kidchenko.github.io',
  base: '/swr-dotnet',

  integrations: [
      starlight({
          title: 'SWR .NET',
          plugins: [
              starlightUtils({
                  multiSidebar: {
                      switcherStyle: 'horizontalList',
                  },
              }),
          ],
          social: [{ icon: 'github', label: 'GitHub', href: 'https://github.com/kidchenko/swr-dotnet' }],
          customCss: ['./src/styles/global.css', './src/styles/starlight-custom.css'],
          sidebar: [
              {
                  label: 'Docs',
                  items: [
                      { label: 'Getting Started', slug: 'docs' },
                      {
                          label: 'Guides',
                          items: [
                              { label: 'Blazor Integration', slug: 'docs/guides/blazor-integration' },
                              { label: 'ASP.NET Core Integration', slug: 'docs/guides/aspnetcore-integration' },
                          ],
                      },
                  ],
              },
              {
                  label: 'API Reference',
                  autogenerate: { directory: 'reference' },
              },
          ],
      }),
	],

  vite: {
    plugins: [tailwindcss()],
  },
});