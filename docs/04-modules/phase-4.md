# Phase 4 — store and recipes

The kitchen store. The Inventory module must be on. It starts off until the super admin grants it.

## Desk

`/inventory` lists ingredients, receives a purchase, and writes a recipe for a dish.

| Unit | Meaning |
| --- | --- |
| g, kg | Weight |
| ml, L | Volume |
| pcs | Count |

A recipe quantity is for one plate, in that ingredient's unit. There is no conversion between g and kg. A bill deducts `recipe quantity × plates`. If Inventory is off, the bill does not touch stock. If it is on and the dish has no recipe, nothing is deducted. On hand may fall below zero so a guest is never refused for an empty bin. A low row is one whose on hand is at or under the reorder level.

Archiving an ingredient is a soft delete and removes its recipe lines. Purchases and sale movements stay.

## Audit

`inventory.item_created`, `inventory.item_updated`, `inventory.item_archived`, `inventory.purchased`, `inventory.adjusted`, `inventory.recipe_saved`, and `inventory.consumed` store the actor and IP. A sale movement also points at the bill.
